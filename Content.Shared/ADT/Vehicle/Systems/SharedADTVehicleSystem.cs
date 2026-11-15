using System.Linq;
using System.Numerics;
using Content.Shared.ActionBlocker;
using Content.Shared.Actions;
using Content.Shared.ADT.Vehicle.Components;
using Content.Shared.Audio;
using Content.Shared.Buckle;
using Content.Shared.Buckle.Components;
using Content.Shared.Foldable;
using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Input;
using Content.Shared.Item;
using Content.Shared.Light.Components;
using Content.Shared.Movement.Events;
using Content.Shared.Popups;
using Content.Shared.Tag;
using Content.Shared.Vehicle;
using Content.Shared.Vehicle.Systems;
using Content.Shared.Weapons.Ranged.Events;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Input.Binding;
using Robust.Shared.Network;
using Robust.Shared.Player;
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
    [Dependency] private SharedEyeSystem _eye = default!;
    [Dependency] private TagSystem _tag = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private SharedActionsSystem _actions = default!;
    [Dependency] private SharedBuckleSystem _buckle = default!;
    [Dependency] private VehicleSystem _vehicle = default!;
    [Dependency] private ActionBlockerSystem _blocker = default!;
    [Dependency] private SharedHandsSystem _hands = default!;

    private const string KeySlot = "key_slot";

    private static readonly ProtoId<TagPrototype> DoorBumpTag = "DoorBumpOpener";
    private static readonly ProtoId<TagPrototype> KeyTag = "VehicleKey";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ADTVehicleComponent, StrapAttemptEvent>(OnStrapAttempt);
        SubscribeLocalEvent<ADTVehicleComponent, VehicleOperatorSetEvent>(OnOperatorSet);
        SubscribeLocalEvent<ADTVehicleComponent, HonkActionEvent>(OnHonkAction);
        SubscribeLocalEvent<ADTVehicleComponent, EntInsertedIntoContainerMessage>(OnEntInserted);
        SubscribeLocalEvent<ADTVehicleComponent, EntRemovedFromContainerMessage>(OnEntRemoved);
        SubscribeLocalEvent<ADTVehicleComponent, MoveEvent>(OnMove);
        SubscribeLocalEvent<ADTVehicleComponent, MoveInputEvent>(OnMoveInput);
        SubscribeLocalEvent<ADTVehicleComponent, FoldedEvent>(OnVehicleFolded);

        SubscribeLocalEvent<ADTInVehicleComponent, GettingPickedUpAttemptEvent>(OnGettingPickedUpAttempt);

        SubscribeLocalEvent<ADTVehicleRiderComponent, ShotAttemptedEvent>(OnShootAttempt);

        CommandBinds.Builder
            .Bind(ContentKeyFunctions.Unbuckle,
                InputCmdHandler.FromDelegate(HandleUnbuckle, handle: false, outsidePrediction: false))
            .Register<SharedADTVehicleSystem>();
    }

    public override void Shutdown()
    {
        base.Shutdown();

        CommandBinds.Unregister<SharedADTVehicleSystem>();
    }

    protected virtual void OnVehicleRotated(Entity<ADTVehicleComponent> ent)
    {
    }

    private Vector2 GetRiderOffset(Entity<ADTVehicleComponent> ent, StrapComponent strap, Angle rotation)
    {
        var baseOffset = strap.BuckleOffset;
        return rotation.GetCardinalDir() switch
        {
            Direction.North => new Vector2(0, ent.Comp.NorthOverride),
            Direction.East => baseOffset,
            Direction.West => new Vector2(-baseOffset.X, baseOffset.Y),
            _ => new Vector2(0, ent.Comp.SouthOverride),
        };
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

        UpdateRiderOffset(ent, Transform(ent));

        if (TryComp<EyeComponent>(rider, out var eye) && eye.Target == null)
            _eye.SetTarget(rider, ent, eye);

        if (TryComp<UnpoweredFlashlightComponent>(ent, out var flashlight))
            _actions.AddAction(rider, ref flashlight.ToggleActionEntity, flashlight.ToggleAction, ent);

        if (ent.Comp.HornSound != null && ent.Comp.HornAction != null)
            _actions.AddAction(rider, ref ent.Comp.HornActionEntity, ent.Comp.HornAction, ent);

        _tag.AddTag(ent, DoorBumpTag);
    }

    private void RemoveRider(Entity<ADTVehicleComponent> ent, EntityUid rider)
    {
        SetAutoAnimate(ent, false);
        _tag.RemoveTag(ent, DoorBumpTag);

        if (TerminatingOrDeleted(rider))
            return;

        if (TryComp<EyeComponent>(rider, out var eye) && eye.Target == ent.Owner)
            _eye.SetTarget(rider, null, eye);

        _actions.RemoveProvidedActions(rider, ent);
        RemComp<ADTVehicleRiderComponent>(rider);
    }

    private void OnMove(Entity<ADTVehicleComponent> ent, ref MoveEvent args)
    {
        if (args.NewRotation.GetCardinalDir() == args.OldRotation.GetCardinalDir())
            return;

        UpdateRiderOffset(ent, args.Component);
        OnVehicleRotated(ent);
    }

    private void UpdateRiderOffset(Entity<ADTVehicleComponent> ent, TransformComponent xform)
    {
        if (_vehicle.GetOperatorOrNull(ent.Owner) is not { } rider
            || !HasComp<ADTVehicleRiderComponent>(rider)
            || !TryComp<StrapComponent>(ent, out var strap))
        {
            return;
        }

        _transform.SetLocalPositionNoLerp(rider, GetRiderOffset(ent, strap, xform.LocalRotation));
    }

    private void OnMoveInput(Entity<ADTVehicleComponent> ent, ref MoveInputEvent args)
    {
        SetAutoAnimate(ent, args.HasDirectionalMovement && args.Entity.Comp.CanMove);
    }

    private void SetAutoAnimate(Entity<ADTVehicleComponent> ent, bool value)
    {
        if (!ent.Comp.AutoAnimate)
            return;

        Appearance.SetData(ent, ADTVehicleVisuals.AutoAnimate, value);
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

    private void OnGettingPickedUpAttempt(Entity<ADTInVehicleComponent> ent, ref GettingPickedUpAttemptEvent args)
    {
        if (ent.Comp.Vehicle is not { } vehicle)
            return;

        if (_vehicle.GetOperatorOrNull(vehicle) is { } rider && rider != args.User)
            args.Cancel();
    }

    private void OnShootAttempt(Entity<ADTVehicleRiderComponent> ent, ref ShotAttemptedEvent args)
    {
        args.Cancel();
    }

    private void HandleUnbuckle(ICommonSession? session)
    {
        if (session?.AttachedEntity is not { } player)
            return;

        if (!TryComp<BuckleComponent>(player, out var buckle) || !buckle.Buckled)
            return;

        if (TryComp<HandsComponent>(player, out var hands)
            && _hands.TryGetHeldItem((player, hands), hands.ActiveHandId, out _, hideVirtualItems: true))
        {
            return;
        }

        if (!_blocker.CanInteract(player, null))
            return;

        _buckle.TryUnbuckle((player, buckle), player, popup: true);
    }
}

[Serializable, NetSerializable]
public enum ADTVehicleVisuals : byte
{
    AutoAnimate,
}

public sealed partial class HonkActionEvent : InstantActionEvent
{
}
