using System.Linq;
using System.Numerics;
using Content.Shared.Actions;
using Content.Shared.ADT.Vehicle.Components;
using Content.Shared.Audio;
using Content.Shared.Buckle;
using Content.Shared.Buckle.Components;
using Content.Shared.Foldable;
using Content.Shared.Item;
using Content.Shared.Light.Components;
using Content.Shared.Movement.Components;
using Content.Shared.Movement.Pulling.Events;
using Content.Shared.Movement.Systems;
using Content.Shared.Popups;
using Content.Shared.Stunnable;
using Content.Shared.Tag;
using Content.Shared.Vehicle;
using Content.Shared.Vehicle.Systems;
using Content.Shared.Weapons.Melee.Events;
using Content.Shared.Weapons.Ranged.Events;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Map;
using Robust.Shared.Network;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.ADT.Vehicle.Systems;

public abstract class SharedADTVehicleSystem : EntitySystem
{
    [Dependency] private INetManager _net = default!;
    [Dependency] protected SharedAppearanceSystem Appearance = default!;
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private SharedAmbientSoundSystem _ambientSound = default!;
    [Dependency] private SharedTransformSystem _transform = default!;
    [Dependency] private TagSystem _tag = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private SharedActionsSystem _actions = default!;
    [Dependency] private SharedJointSystem _joints = default!;
    [Dependency] private SharedBuckleSystem _buckle = default!;
    [Dependency] private SharedMoverController _mover = default!;
    [Dependency] private SharedStunSystem _stun = default!;
    [Dependency] private VehicleSystem _vehicle = default!;

    private const string KeySlot = "key_slot";

    private static readonly ProtoId<TagPrototype> DoorBumpTag = "DoorBumpOpener";
    private static readonly ProtoId<TagPrototype> KeyTag = "VehicleKey";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ADTVehicleComponent, ComponentStartup>(OnVehicleStartup);
        SubscribeLocalEvent<ADTVehicleComponent, StrapAttemptEvent>(OnStrapAttempt);
        SubscribeLocalEvent<ADTVehicleComponent, VehicleOperatorSetEvent>(OnOperatorSet);
        SubscribeLocalEvent<ADTVehicleComponent, HonkActionEvent>(OnHonkAction);
        SubscribeLocalEvent<ADTVehicleComponent, EntInsertedIntoContainerMessage>(OnEntInserted);
        SubscribeLocalEvent<ADTVehicleComponent, EntRemovedFromContainerMessage>(OnEntRemoved);
        SubscribeLocalEvent<ADTVehicleComponent, MoveEvent>(OnMoveEvent);
        SubscribeLocalEvent<ADTVehicleComponent, FoldedEvent>(OnVehicleFolded);

        SubscribeLocalEvent<ADTInVehicleComponent, GettingPickedUpAttemptEvent>(OnGettingPickedUpAttempt);

        SubscribeLocalEvent<ADTVehicleRiderComponent, PullAttemptEvent>(OnPullAttempt);
        SubscribeLocalEvent<ADTVehicleRiderComponent, ShotAttemptedEvent>(OnShootAttempt);
        SubscribeLocalEvent<ADTVehicleRiderComponent, MeleeHitEvent>(OnHitAttempt);
    }

    public override void Update(float frameTime)
    {
        var query = EntityQueryEnumerator<ADTVehicleComponent, InputMoverComponent>();
        while (query.MoveNext(out var uid, out var vehicle, out var mover))
        {
            if (!vehicle.AutoAnimate)
                continue;

            UpdateAutoAnimate(uid, _mover.GetVelocityInput(mover).Sprinting != Vector2.Zero);
        }
    }

    private void OnVehicleStartup(Entity<ADTVehicleComponent> ent, ref ComponentStartup args)
    {
        UpdateDrawDepth(ent, 2);

        if (TryComp<StrapComponent>(ent, out var strap))
        {
            ent.Comp.BaseBuckleOffset = strap.BuckleOffset;
            strap.BuckleOffset = Vector2.Zero;
        }
    }

    private void OnStrapAttempt(Entity<ADTVehicleComponent> ent, ref StrapAttemptEvent args)
    {
        if (!TryComp<FoldableComponent>(ent, out var foldable) || !foldable.IsFolded)
            return;

        args.Cancelled = true;
        if (args.Popup && args.User != null)
        {
            _popup.PopupClient(Loc.GetString("vehicle-folded-cannot-buckle", ("vehicle", ent.Owner)),
                args.User,
                PopupType.Medium);
        }
    }

    private void OnOperatorSet(Entity<ADTVehicleComponent> ent, ref VehicleOperatorSetEvent args)
    {
        if (args.OldOperator is { } oldRider)
            RemoveRider(ent, oldRider);

        if (args.NewOperator is { } newRider)
            SetupRider(ent, newRider);
    }

    private void SetupRider(Entity<ADTVehicleComponent> ent, EntityUid rider)
    {
        var riderComp = EnsureComp<ADTVehicleRiderComponent>(rider);
        riderComp.Vehicle = ent;
        Dirty(rider, riderComp);

        ent.Comp.LastRider = rider;
        Dirty(ent);
        Appearance.SetData(ent, ADTVehicleVisuals.HideRider, true);

        UpdateBuckleOffset(ent, Transform(ent));
        if (TryComp<InputMoverComponent>(ent, out var mover))
            UpdateDrawDepth(ent, GetDrawDepth(Transform(ent), ent.Comp, mover.RelativeRotation));

        if (TryComp<UnpoweredFlashlightComponent>(ent, out var flashlight))
            _actions.AddAction(rider, ref flashlight.ToggleActionEntity, flashlight.ToggleAction, ent);

        if (ent.Comp.HornSound != null && ent.Comp.HornAction != null)
            _actions.AddAction(rider, ref ent.Comp.HornActionEntity, ent.Comp.HornAction, ent);

        _joints.ClearJoints(rider);
        _tag.AddTag(ent, DoorBumpTag);
    }

    private void RemoveRider(Entity<ADTVehicleComponent> ent, EntityUid rider)
    {
        Appearance.SetData(ent, ADTVehicleVisuals.HideRider, false);

        if (TerminatingOrDeleted(rider))
            return;

        _actions.RemoveProvidedActions(rider, ent);
        RemComp<ADTVehicleRiderComponent>(rider);
        _tag.RemoveTag(ent, DoorBumpTag);
    }

    private void OnVehicleFolded(Entity<ADTVehicleComponent> ent, ref FoldedEvent args)
    {
        if (!args.IsFolded || _vehicle.GetOperatorOrNull(ent.Owner) is not { } rider)
            return;

        if (TryComp<StrapComponent>(ent, out var strap))
        {
            foreach (var buckled in strap.BuckledEntities.ToArray())
            {
                _buckle.Unbuckle(buckled, null);
            }
        }

        if (!TerminatingOrDeleted(rider))
        {
            _popup.PopupClient(Loc.GetString("vehicle-folded-ejected", ("vehicle", ent.Owner)),
                rider,
                PopupType.Medium);
        }
    }

    private void OnHonkAction(Entity<ADTVehicleComponent> ent, ref HonkActionEvent args)
    {
        if (args.Handled || ent.Comp.HornSound == null)
            return;

        _audio.PlayPredicted(ent.Comp.HornSound, ent, args.Performer);
        args.Handled = true;
    }

    private void OnEntInserted(Entity<ADTVehicleComponent> ent, ref EntInsertedIntoContainerMessage args)
    {
        if (args.Container.ID != KeySlot || !_tag.HasTag(args.Entity, KeyTag))
            return;

        var inVehicle = EnsureComp<ADTInVehicleComponent>(args.Entity);
        inVehicle.Vehicle = ent;
        Dirty(args.Entity, inVehicle);

        if (_net.IsServer)
        {
            _popup.PopupEntity(Loc.GetString("vehicle-use-key", ("keys", args.Entity), ("vehicle", ent.Owner)),
                ent,
                args.OldParent,
                PopupType.Medium);
        }

        _ambientSound.SetAmbience(ent, true);
    }

    private void OnEntRemoved(Entity<ADTVehicleComponent> ent, ref EntRemovedFromContainerMessage args)
    {
        if (args.Container.ID != KeySlot || !RemComp<ADTInVehicleComponent>(args.Entity))
            return;

        _ambientSound.SetAmbience(ent, false);
    }

    private void OnMoveEvent(Entity<ADTVehicleComponent> ent, ref MoveEvent args)
    {
        if (args.NewRotation == args.OldRotation)
            return;

        if (ent.Comp.AutoAnimate && !HasComp<InputMoverComponent>(ent))
        {
            UpdateAutoAnimate(ent, false);
            return;
        }

        UpdateBuckleOffset(ent, args.Component);
        if (TryComp<InputMoverComponent>(ent, out var mover))
            UpdateDrawDepth(ent, GetDrawDepth(args.Component, ent.Comp, mover.RelativeRotation));
    }

    private void OnGettingPickedUpAttempt(Entity<ADTInVehicleComponent> ent, ref GettingPickedUpAttemptEvent args)
    {
        if (ent.Comp.Vehicle is not { } vehicle)
        {
            args.Cancel();
            return;
        }

        if (_vehicle.GetOperatorOrNull(vehicle) is { } rider && rider != args.User)
            args.Cancel();
    }

    private void OnPullAttempt(Entity<ADTVehicleRiderComponent> ent, ref PullAttemptEvent args)
    {
        if (ent.Comp.Vehicle != null)
            args.Cancelled = true;
    }

    private void OnShootAttempt(Entity<ADTVehicleRiderComponent> ent, ref ShotAttemptedEvent args)
    {
        args.Cancel();
    }

    private void OnHitAttempt(Entity<ADTVehicleRiderComponent> ent, ref MeleeHitEvent args)
    {
        _stun.TryKnockdown(ent.Owner, TimeSpan.FromSeconds(4), refresh: false);
    }

    private int GetDrawDepth(TransformComponent xform, ADTVehicleComponent component, Angle cameraAngle)
    {
        var itemDirection = cameraAngle.GetDir() switch
        {
            Direction.South => xform.LocalRotation.GetDir(),
            Direction.North => xform.LocalRotation.RotateDir(Direction.North),
            Direction.West => xform.LocalRotation.RotateDir(Direction.East),
            Direction.East => xform.LocalRotation.RotateDir(Direction.West),
            _ => Direction.South,
        };

        var over = itemDirection switch
        {
            Direction.North => component.NorthOver,
            Direction.South => component.SouthOver,
            Direction.West => component.WestOver,
            Direction.East => component.EastOver,
            _ => false,
        };

        return over ? (int) DrawDepth.DrawDepth.Doors : (int) DrawDepth.DrawDepth.WallMountedItems;
    }

    private void UpdateBuckleOffset(Entity<ADTVehicleComponent> ent, TransformComponent xform)
    {
        if (!TryComp<StrapComponent>(ent, out var strap))
            return;

        var component = ent.Comp;
        var oldOffset = strap.BuckleOffset;
        var rot = xform.LocalRotation.Degrees;
        if (rot > 0)
        {
            strap.BuckleOffset = rot switch
            {
                < 45f => new(0, component.SouthOverride),
                <= 145f => component.BaseBuckleOffset,
                < 225f => new(0, component.NorthOverride),
                <= 325f => new(component.BaseBuckleOffset.X * -1, component.BaseBuckleOffset.Y),
                _ => new(0, component.SouthOverride),
            };
        }
        else
        {
            strap.BuckleOffset = rot switch
            {
                > -45f => new(0, component.SouthOverride),
                >= -145f => new(component.BaseBuckleOffset.X * -1, component.BaseBuckleOffset.Y),
                > -225f => new(0, component.NorthOverride),
                >= -325f => component.BaseBuckleOffset,
                _ => new(0, component.SouthOverride),
            };
        }

        if (!oldOffset.Equals(strap.BuckleOffset) && _net.IsServer)
            Dirty(ent, strap);

        foreach (var buckledEntity in strap.BuckledEntities)
        {
            if (_net.IsServer)
            {
                var coords = new EntityCoordinates(ent, strap.BuckleOffset);
                _transform.SetCoordinates(buckledEntity, Transform(buckledEntity), coords);
            }
            else
            {
                _transform.SetLocalPositionNoLerp(buckledEntity, strap.BuckleOffset);
            }
        }
    }

    private void UpdateDrawDepth(EntityUid uid, int drawDepth)
    {
        Appearance.SetData(uid, ADTVehicleVisuals.DrawDepth, drawDepth);
    }

    private void UpdateAutoAnimate(EntityUid uid, bool autoAnimate)
    {
        Appearance.SetData(uid, ADTVehicleVisuals.AutoAnimate, autoAnimate);
    }
}

[Serializable, NetSerializable]
public enum ADTVehicleVisuals : byte
{
    DrawDepth,
    AutoAnimate,
    HideRider,
}

public sealed partial class HonkActionEvent : InstantActionEvent
{
}
