// SPDX-FileCopyrightText: 2025 Aiden <28298836+Aidenkrz@users.noreply.github.com>
// SPDX-FileCopyrightText: 2025 Aiden <aiden@djkraz.com>
// SPDX-FileCopyrightText: 2025 Fishbait <Fishbait@git.ml>
// SPDX-FileCopyrightText: 2025 Rinary <72972221+Rinary1@users.noreply.github.com>
// SPDX-FileCopyrightText: 2025 fishbait <gnesse@gmail.com>
// SPDX-FileCopyrightText: 2025 ss14-Starlight <ss14-Starlight@outlook.com>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Linq;
using Content.Shared.Actions;
using Content.Shared.Body;
using Content.Shared.Body.Components;
using Content.Shared.Eye;
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
    [Dependency] private readonly SharedActionsSystem _actions = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<VentCrawlerHolderComponent, ComponentStartup>(OnComponentStartup);
        SubscribeLocalEvent<VentCrawlerHolderComponent, MoveInputEvent>(OnMoveInput);
        SubscribeLocalEvent<VentCrawlerComponent, GetVisMaskEvent>(OnGetVisMask);
        SubscribeLocalEvent<BeingVentCrawlerComponent, ExitVentActionEvent>(OnExitAction);
    }

    private void OnGetVisMask(Entity<VentCrawlerComponent> ent, ref GetVisMaskEvent args)
    {
        if (ent.Comp.InTube)
            args.VisibilityMask |= (int) VisibilityFlags.Subfloor;
    }

    private void OnExitAction(EntityUid uid, BeingVentCrawlerComponent component, ExitVentActionEvent args)
    {
        if (args.Handled || !TryComp<VentCrawlerHolderComponent>(component.Holder, out var holder))
            return;

        if (holder.CurrentTube is not {} tube || !HasComp<VentCrawlerEntryComponent>(tube))
            return;

        if (TryComp<WeldableComponent>(tube, out var weldable) && weldable.IsWelded)
            return;

        var ev = new VentCrawlingExitEvent();
        RaiseLocalEvent(component.Holder, ref ev);
        args.Handled = true;
    }

    private void UpdateExitAction(VentCrawlerHolderComponent holder)
    {
        var player = holder.Container.ContainedEntities.FirstOrDefault();
        if (player == default)
            return;

        var atExit = holder.CurrentTube is {} tube
            && HasComp<VentCrawlerEntryComponent>(tube)
            && (!TryComp<WeldableComponent>(tube, out var weldable) || !weldable.IsWelded);

        if (atExit)
        {
            if (holder.ExitAction == null)
                holder.ExitAction = _actions.AddAction(player, "VentCrawlExitAction");
        }
        else if (holder.ExitAction is {} action)
        {
            _actions.RemoveAction(player, action);
            holder.ExitAction = null;
        }
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
        if (!Exists(holder.CurrentTube))
        {
            var ev = new VentCrawlingExitEvent();
            RaiseLocalEvent(uid, ref ev);
            return;
        }

        holder.DesiredDirection = MoveButtonsToDirectionFast(args.Entity.Comp.HeldMoveButtons);
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
            if (holder.CurrentTube == null || !Exists(holder.CurrentTube.Value))
            {
                var exitEv = new VentCrawlingExitEvent();
                RaiseLocalEvent(uid, ref exitEv);
                continue;
            }

            if (!_containerSystem.IsEntityInContainer(holder.Container.Owner))
            {
                var exitEv = new VentCrawlingExitEvent();
                RaiseLocalEvent(uid, ref exitEv);
                continue;
            }

            UpdateExitAction(holder);

            if (holder.NextTube == null && holder.DesiredDirection != Direction.Invalid)
            {
                if (TryStartSegment(uid, holder))
                    holder.Progress = 0;
            }

            if (holder.NextTube == null)
                continue;

            holder.Progress += frameTime / holder.Speed;

            while (holder.Progress >= 1)
            {
                holder.Progress -= 1;
                MoveToNextTube(uid, holder);

                if (holder.Progress > 0 && holder.DesiredDirection != Direction.Invalid && !TryStartSegment(uid, holder))
                    break;
            }

            SetSegmentPosition(uid, holder);
        }
    }

    private bool TryStartSegment(EntityUid uid, VentCrawlerHolderComponent holder)
    {
        if (holder.DesiredDirection == Direction.Invalid)
        {
            holder.CurrentDirection = Direction.Invalid;
            return false;
        }

        var currentTube = holder.CurrentTube!.Value;
        var direction = ResolveDirection(currentTube, holder.CurrentDirection, holder.DesiredDirection);

        if (direction == Direction.Invalid)
        {
            holder.CurrentDirection = Direction.Invalid;
            return false;
        }

        var nextTube = _VentCrawlerTubeSystem.NextTubeFor(currentTube, direction);

        if (nextTube == null)
        {
            if (!HasComp<VentCrawlerEntryComponent>(currentTube))
            {
                var ev = new GetVentCrawlingsConnectableDirectionsEvent();
                RaiseLocalEvent(currentTube, ref ev);
                if (ev.Connectable != null && ev.Connectable.Contains(direction))
                {
                    var exitEv = new VentCrawlingExitEvent();
                    RaiseLocalEvent(uid, ref exitEv);
                }
            }

            holder.CurrentDirection = Direction.Invalid;
            return false;
        }

        holder.CurrentDirection = direction;
        holder.NextTube = nextTube;
        return true;
    }

    private void MoveToNextTube(EntityUid uid, VentCrawlerHolderComponent holder)
    {
        if (holder.NextTube == null)
            return;

        var currentTube = holder.CurrentTube!.Value;

        _containerSystem.Remove(uid, Comp<VentCrawlerTubeComponent>(currentTube).Contents, reparent: false, force: true);

        if (_gameTiming.CurTime > holder.LastCrawl + VentCrawlerHolderComponent.CrawlDelay)
        {
            holder.LastCrawl = _gameTiming.CurTime;
            _audioSystem.PlayPvs(holder.CrawlSound, uid);
        }

        EnterTube(uid, holder.NextTube.Value, holder);
        holder.NextTube = null;
    }

    private void SetSegmentPosition(EntityUid uid, VentCrawlerHolderComponent holder)
    {
        if (holder.NextTube == null)
            return;

        var origin = Transform(holder.CurrentTube!.Value).Coordinates;
        var target = Transform(holder.NextTube.Value).Coordinates;
        var newPosition = (target.Position - origin.Position) * holder.Progress;

        _xformSystem.SetCoordinates(uid, _xformSystem.WithEntityId(origin.Offset(newPosition), holder.CurrentTube.Value));
    }

    private Direction ResolveDirection(EntityUid tube, Direction current, Direction desired)
    {
        if (desired == Direction.Invalid)
            return Direction.Invalid;

        var ev = new GetVentCrawlingsConnectableDirectionsEvent();
        RaiseLocalEvent(tube, ref ev);

        if (ev.Connectable == null)
            return desired;

        if (ev.Connectable.Contains(desired))
            return desired;

        if (IsDiagonal(desired))
        {
            GetCardinalAxes(desired, out var axis1, out var axis2);

            if (axis1 != current && ev.Connectable.Contains(axis1))
                return axis1;
            if (axis2 != current && ev.Connectable.Contains(axis2))
                return axis2;

            if (ev.Connectable.Contains(axis1))
                return axis1;
            if (ev.Connectable.Contains(axis2))
                return axis2;

            return Direction.Invalid;
        }

        if (current != Direction.Invalid && ev.Connectable.Contains(current))
            return current;

        return Direction.Invalid;
    }

    private static bool IsDiagonal(Direction direction)
        => (int) direction % 2 == 1;

    private static void GetCardinalAxes(Direction diagonal, out Direction axis1, out Direction axis2)
    {
        var vec = diagonal.ToIntVec();
        axis1 = vec.X > 0 ? Direction.East : Direction.West;
        axis2 = vec.Y > 0 ? Direction.North : Direction.South;
    }
}
