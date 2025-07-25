// SPDX-FileCopyrightText: 2025 Aiden <28298836+Aidenkrz@users.noreply.github.com>
// SPDX-FileCopyrightText: 2025 Aiden <aiden@djkraz.com>
// SPDX-FileCopyrightText: 2025 Fishbait <Fishbait@git.ml>
// SPDX-FileCopyrightText: 2025 Rinary <72972221+Rinary1@users.noreply.github.com>
// SPDX-FileCopyrightText: 2025 fishbait <gnesse@gmail.com>
// SPDX-FileCopyrightText: 2025 ss14-Starlight <ss14-Starlight@outlook.com>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Linq;
using Content.Shared.Body.Components;
using Content.Shared.Tools.Components;
using Content.Shared.Item;
using Content.Shared.Movement.Events;
using Content.Shared.ADT.VentCrawling.Components;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Timing;
using Content.Shared.Movement.Systems;

namespace Content.Shared.ADT.VentCrawling;

/// <summary>
/// A system that handles the crawling behavior for vent creatures.
/// </summary>
public sealed class SharedVentCrawableSystem : EntitySystem
{
    [Dependency] private readonly SharedVentTubeSystem _VentCrawlerTubeSystem = default!;
    [Dependency] private readonly SharedPhysicsSystem _physicsSystem = default!;
    [Dependency] private readonly SharedContainerSystem _containerSystem = default!;
    [Dependency] private readonly SharedTransformSystem _xformSystem = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly SharedAudioSystem _audioSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<VentCrawlerHolderComponent, ComponentStartup>(OnComponentStartup);
        SubscribeLocalEvent<VentCrawlerHolderComponent, MoveInputEvent>(OnMoveInput);
    }
    private static readonly Direction[] _validMoveButtonsToDirection = new Direction[16]
    {
        Direction.Invalid,        // 0: None
        Direction.North,          // 1: Up
        Direction.South,          // 2: Down
        Direction.Invalid,        // 3: Up | Down (недопустимо)
        Direction.West,           // 4: Left
        Direction.NorthWest,      // 5: Up | Left
        Direction.SouthWest,      // 6: Down | Left
        Direction.Invalid,        // 7: Up | Down | Left
        Direction.East,           // 8: Right
        Direction.NorthEast,      // 9: Up | Right
        Direction.SouthEast,      // 10: Down | Right
        Direction.Invalid,        // 11: Up | Down | Right
        Direction.Invalid,        // 12: Left | Right (недопустимо)
        Direction.Invalid,        // 13: Up | Left | Right
        Direction.Invalid,        // 14: Down | Left | Right
        Direction.Invalid,        // 15: Up | Down | Left | Right
    };

    public static Direction MoveButtonsToDirectionFast(MoveButtons buttons)
    {
        return _validMoveButtonsToDirection[(byte)(buttons & MoveButtons.AnyDirection)];
    }
    /// <summary>
    /// Handles the MoveInputEvent for VentCrawlerHolderComponent.
    /// </summary>
    /// <param name="uid">The EntityUid of the VentCrawlerHolderComponent.</param>
    /// <param name="component">The VentCrawlerHolderComponent instance.</param>
    /// <param name="args">The MoveInputEvent arguments.</param>
    private void OnMoveInput(EntityUid uid, VentCrawlerHolderComponent holder, ref MoveInputEvent args)
    {

        if (!EntityManager.EntityExists(holder.CurrentTube))
        {
            var ev = new VentCrawlingExitEvent();
            RaiseLocalEvent(uid, ref ev);
        }
        holder.IsMoving = args.HasDirectionalMovement;
        holder.CurrentDirection = MoveButtonsToDirectionFast(args.Entity.Comp.HeldMoveButtons);
    }

    /// <summary>
    /// Handles the ComponentStartup event for VentCrawlerHolderComponent.
    /// </summary>
    /// <param name="uid">The EntityUid of the VentCrawlerHolderComponent.</param>
    /// <param name="holder">The VentCrawlerHolderComponent instance.</param>
    /// <param name="args">The ComponentStartup arguments.</param>
    private void OnComponentStartup(EntityUid uid, VentCrawlerHolderComponent holder, ComponentStartup args)
        => holder.Container = _containerSystem.EnsureContainer<Container>(uid, nameof(VentCrawlerHolderComponent));

    /// <summary>
    /// Tries to insert an entity into the VentCrawlerHolderComponent container.
    /// </summary>
    /// <param name="uid">The EntityUid of the VentCrawlerHolderComponent.</param>
    /// <param name="toInsert">The EntityUid of the entity to insert.</param>
    /// <param name="holder">The VentCrawlerHolderComponent instance.</param>
    /// <returns>True if the insertion was successful, otherwise False.</returns>
    public bool TryInsert(EntityUid uid, EntityUid toInsert, VentCrawlerHolderComponent? holder = null)
    {
        if (!Resolve(uid, ref holder))
            return false;

        if (!CanInsert(uid, toInsert, holder))
            return false;

        if (!_containerSystem.Insert(toInsert, holder.Container))
            return false;

        if (TryComp<PhysicsComponent>(toInsert, out var physBody))
            _physicsSystem.SetCanCollide(toInsert, false, body: physBody);

        return true;
    }

    /// <summary>
    /// Checks whether the specified entity can be inserted into the container of the VentCrawlerHolderComponent.
    /// </summary>
    /// <param name="uid">The EntityUid of the VentCrawlerHolderComponent.</param>
    /// <param name="toInsert">The EntityUid of the entity to be inserted.</param>
    /// <param name="holder">The VentCrawlerHolderComponent instance.</param>
    /// <returns>True if the entity can be inserted into the container; otherwise, False.</returns>
    private bool CanInsert(EntityUid uid, EntityUid toInsert, VentCrawlerHolderComponent? holder = null)
    {
        if (!Resolve(uid, ref holder))
            return false;

        if (!_containerSystem.CanInsert(toInsert, holder.Container))
            return false;

        return HasComp<ItemComponent>(toInsert) ||
            HasComp<BodyComponent>(toInsert);
    }

    /// <summary>
    /// Attempts to make the VentCrawlerHolderComponent enter a VentCrawlerTubeComponent.
    /// </summary>
    /// <param name="holderUid">The EntityUid of the VentCrawlerHolderComponent.</param>
    /// <param name="toUid">The EntityUid of the VentCrawlerTubeComponent to enter.</param>
    /// <param name="holder">The VentCrawlerHolderComponent instance.</param>
    /// <param name="holderTransform">The TransformComponent instance for the VentCrawlerHolderComponent.</param>
    /// <param name="to">The VentCrawlerTubeComponent instance to enter.</param>
    /// <param name="toTransform">The TransformComponent instance for the VentCrawlerTubeComponent.</param>
    /// <returns>True if the VentCrawlerHolderComponent successfully enters the VentCrawlerTubeComponent; otherwise, False.</returns>
    public bool EnterTube(EntityUid holderUid, EntityUid toUid, VentCrawlerHolderComponent? holder = null, TransformComponent? holderTransform = null, VentCrawlerTubeComponent? to = null, TransformComponent? toTransform = null)
    {
        if (!Resolve(holderUid, ref holder, ref holderTransform))
            return false;

        if (holder.IsExitingVentCraws)
        {
            Log.Error("Tried entering tube after exiting VentCraws. This should never happen.");
            return false;
        }

        if (!Resolve(toUid, ref to, ref toTransform))
        {
            var ev = new VentCrawlingExitEvent();
            RaiseLocalEvent(holderUid, ref ev);
            return false;
        }

        foreach (var ent in holder.Container.ContainedEntities)
        {
            var comp = EnsureComp<BeingVentCrawlerComponent>(ent);
            comp.Holder = holderUid;
        }

        if (!_containerSystem.Insert(holderUid, to.Contents))
        {
            var ev = new VentCrawlingExitEvent();
            RaiseLocalEvent(holderUid, ref ev);
            return false;
        }

        if (TryComp<PhysicsComponent>(holderUid, out var physBody))
            _physicsSystem.SetCanCollide(holderUid, false, body: physBody);

        if (holder.CurrentTube != null)
        {
            holder.PreviousTube = holder.CurrentTube;
            holder.PreviousDirection = holder.CurrentDirection;
        }

        holder.CurrentTube = toUid;

        return true;
    }

    /// <summary>
    ///  Magic...
    /// </summary>
    public override void Update(float frameTime)
    {
        var query = EntityQueryEnumerator<VentCrawlerHolderComponent>();
        while (query.MoveNext(out var uid, out var holder))
        {
            if (holder.CurrentDirection == Direction.Invalid || holder.CurrentTube == null)
                continue;

            var currentTube = holder.CurrentTube.Value;

            if (holder.IsMoving && holder.NextTube == null)
            {
                var nextTube = _VentCrawlerTubeSystem.NextTubeFor(currentTube, holder.CurrentDirection);

                if (nextTube != null)
                {
                    if (!EntityManager.EntityExists(holder.CurrentTube))
                    {
                        var ev = new VentCrawlingExitEvent();
                        RaiseLocalEvent(uid, ref ev);
                        continue;
                    }

                    holder.NextTube = nextTube;
                    holder.StartingTime = holder.Speed;
                    holder.TimeLeft = holder.Speed;
                }
                else
                {
                    var ev = new GetVentCrawlingsConnectableDirectionsEvent();
                    RaiseLocalEvent(currentTube, ref ev);
                    if (ev.Connectable.Contains(holder.CurrentDirection))
                    {
                        var Exitev = new VentCrawlingExitEvent();
                        RaiseLocalEvent(uid, ref Exitev);
                        continue;
                    }
                }
            }

            if (holder.NextTube != null && holder.TimeLeft > 0)
            {
                var time = frameTime;
                if (time > holder.TimeLeft)
                    time = holder.TimeLeft;

                var progress = 1 - holder.TimeLeft / holder.StartingTime;
                var origin = Transform(currentTube).Coordinates;
                var target = Transform(holder.NextTube.Value).Coordinates;
                var newPosition = (target.Position - origin.Position) * progress;

                _xformSystem.SetCoordinates(uid, origin.Offset(newPosition).WithEntityId(currentTube));

                holder.TimeLeft -= time;
                frameTime -= time;
            }
            else if (holder.NextTube != null && holder.TimeLeft == 0)
            {
                var welded = false;

                if (TryComp<WeldableComponent>(holder.NextTube.Value, out var weldableComponent))
                    welded = weldableComponent.IsWelded;

                if (HasComp<VentCrawlerEntryComponent>(holder.NextTube.Value) && !holder.FirstEntry && !welded)
                {
                    var ev = new VentCrawlingExitEvent();
                    RaiseLocalEvent(uid, ref ev);
                }
                else
                {
                    _containerSystem.Remove(uid, Comp<VentCrawlerTubeComponent>(currentTube).Contents ,reparent: false, force: true);

                    if (holder.FirstEntry)
                        holder.FirstEntry = false;

                    if (_gameTiming.CurTime > holder.LastCrawl + VentCrawlerHolderComponent.CrawlDelay)
                    {
                        holder.LastCrawl = _gameTiming.CurTime;
                        _audioSystem.PlayPvs(holder.CrawlSound, uid);
                    }

                    EnterTube(uid, holder.NextTube.Value, holder);
                    holder.NextTube = null;
                }
            }
        }
    }
}
