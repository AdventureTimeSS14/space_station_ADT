//

using Content.Shared.ADT.Heretic.Common;
using Content.Shared.Doors;
using Content.Shared.Doors.Components;
using Content.Shared.Doors.Systems;
using Content.Shared.Heretic.Components.PathSpecific.Lock;
using Content.Shared.ADT.Heretic.Systems;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Events;
using Content.Shared.Mobs.Systems;
using Content.Shared.Popups;
using Content.Shared.Silicons.StationAi;
using Content.Shared.StatusEffect;
using Content.Shared.Stunnable;
using Content.Shared.Weapons.Ranged.Systems;
using Content.Shared.Damage.Systems;
using Content.Shared.DoAfter;
using Content.Shared.Ghost;
using Content.Shared.Jittering;
using Content.Shared.Power.EntitySystems;
using Content.Shared.Singularity.Components;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Serialization;
using Robust.Shared.Timing;
using System.Numerics;

namespace Content.Shared.ADT.Heretic.Systems.PathSpecific.Lock;

public sealed partial class SerpentclaveSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly LockPortalSystem _portal = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly SharedHereticSystem _heretic = default!;
    [Dependency] private readonly SharedStationAiSystem _ai = default!;
    [Dependency] private readonly SharedDoorSystem _door = default!;
    [Dependency] private readonly SharedGunSystem _gun = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedJitteringSystem _jitter = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly SharedPowerReceiverSystem _power = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var now = _timing.CurTime;
        var query = EntityQueryEnumerator<LockTrappedDoorComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if (now < comp.NextGrapple)
                continue;

            Grapple((uid, comp));
        }
    }

    private void OnInteractAttempt(Entity<LockTrappedDoorComponent> ent, ref GettingInteractedWithAttemptEvent args)
    {
        if (_heretic.IsHereticOrGhoul(args.Uid))
            return;

        args.Cancelled = true;
        AggroTrappedDoor(ent, args.Uid);
    }

    private void OnBeforeDoorOpen(Entity<LockTrappedDoorComponent> ent, ref BeforeDoorOpenedEvent args)
    {
        if (args.User is { } user && !_mobState.IsAlive(user) && !HasComp<GhostComponent>(user))
            args.Cancel();
    }

    private void OnOpen(Entity<LockTrappedDoorComponent> ent, ref DoorStateChangedEvent args)
    {
        if (args.State != DoorState.Open)
            return;

        var nearby = _lookup.GetEntitiesInRange(ent, 1.5f);
        foreach (var uid in nearby)
        {
            if (_heretic.IsHereticOrGhoul(uid) || HasComp<StunnedComponent>(uid) || !_mobState.IsAlive(uid))
                continue;

            AggroTrappedDoor(ent, uid);
            break;
        }
    }

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<LockTrappedDoorComponent, GettingInteractedWithAttemptEvent>(OnInteractAttempt);
        SubscribeLocalEvent<LockTrappedDoorComponent, BeforeDoorOpenedEvent>(OnBeforeDoorOpen);
        SubscribeLocalEvent<LockTrappedDoorComponent, DoorStateChangedEvent>(OnOpen);
        SubscribeLocalEvent<LockTrappedDoorComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<LockTrappedDoorComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<LockTrappedDoorComponent, BeforeDoorClosedEvent>(OnBeforeDoorClosed);
        SubscribeLocalEvent<SerpentclaveComponent, AfterInteractEvent>(OnAfterInteract);
        SubscribeLocalEvent<SerpentclaveComponent, SerpentclaveDoAfterEvent>(OnDoAfter);
    }

    private void OnShutdown(Entity<LockTrappedDoorComponent> ent, ref ComponentShutdown args)
    {
        if (TerminatingOrDeleted(ent))
            return;

        RemCompDeferred<ContainmentFieldComponent>(ent);
        _power.SetNeedsPower(ent, true);
    }

    private void OnMapInit(Entity<LockTrappedDoorComponent> ent, ref MapInitEvent args)
    {
        if (TryComp(ent, out StationAiWhitelistComponent? whitelist))
            _ai.SetWhitelistEnabled((ent, whitelist), false);

        _power.SetNeedsPower(ent, false);

        var field = EnsureComp<ContainmentFieldComponent>(ent);
        var status = EnsureComp<StatusEffectsComponent>(ent);

        status.AllowedEffects ??= new();
        if (!status.AllowedEffects.Contains("Jitter"))
            status.AllowedEffects.Add("Jitter");

        _jitter.DoJitter(ent, ent.Comp.JitterTime, false, status: status);
    }

    private void OnBeforeDoorClosed(Entity<LockTrappedDoorComponent> ent, ref BeforeDoorClosedEvent args)
    {
        if (ent.Comp.GrappleTarget != null)
        {
            args.Cancel();
            return;
        }

        args.PerformCollisionCheck = false;
    }

    private void OnDoAfter(Entity<SerpentclaveComponent> ent, ref SerpentclaveDoAfterEvent args)
    {
        if (args.Cancelled || args.Handled)
            return;

        args.Handled = true;

        if (args.Target is not { } target)
            return;

        if (_portal.IsDoorOccupied(target, args.User))
            return;

        var comp = EnsureComp<LockTrappedDoorComponent>(target);

        if (_heretic.IsHereticOrGhoul(args.User))
        {
            _audio.PlayPredicted(comp.AggroSound, target, args.User);
            return;
        }

        AggroTrappedDoor((target, comp), args.User);
    }

    private void OnAfterInteract(Entity<SerpentclaveComponent> ent, ref AfterInteractEvent args)
    {
        if (args.Target is not { } target || !HasComp<AirlockComponent>(args.Target))
            return;

        var doArgs = new DoAfterArgs(EntityManager,
            args.User,
            ent.Comp.DoAfterTime,
            new SerpentclaveDoAfterEvent(),
            ent,
            target,
            ent)
        {
            BreakOnMove = true,
            BreakOnDamage = true,
            NeedHand = true,
            BreakOnWeightlessMove = false,
        };

        if (_doAfter.TryStartDoAfter(doArgs))
            args.Handled = true;
    }

    private void Grapple(Entity<LockTrappedDoorComponent> ent)
    {
        if (ent.Comp.GrappleTarget is not { } target)
            return;

        ent.Comp.GrappleTarget = null;
        Dirty(ent);

        var proj = SpawnAtPosition(ent.Comp.ProjectileProto, Transform(ent).Coordinates);
        var projPos = _transform.GetWorldPosition(proj);
        var targetPos = _transform.GetWorldPosition(target);

        var dir = (targetPos - projPos).Normalized();

        _gun.ShootProjectile(proj, dir, Vector2.Zero, ent, ent);
    }

    private void AggroTrappedDoor(Entity<LockTrappedDoorComponent> ent, EntityUid target)
    {
        if (!TryComp(ent, out DoorComponent? door) || !_mobState.IsAlive(target))
            return;

        if (TryComp(ent, out DoorBoltComponent? bolt))
            _door.SetBoltsDown((ent, bolt), false);

        _audio.PlayPredicted(ent.Comp.AggroSound, ent, target);

        if (door.State == DoorState.Open)
        {
            ent.Comp.GrappleTarget = target;
            Grapple(ent);
            return;
        }

        if (!_door.TryOpen(ent))
            return;

        if (ent.Comp.GrappleTarget != null)
            return;

        ent.Comp.GrappleTarget = target;
        ent.Comp.NextGrapple = _timing.CurTime + ent.Comp.GrappleDelay;
        Dirty(ent);
    }
}

[Serializable, NetSerializable]
public sealed partial class SerpentclaveDoAfterEvent : SimpleDoAfterEvent;
