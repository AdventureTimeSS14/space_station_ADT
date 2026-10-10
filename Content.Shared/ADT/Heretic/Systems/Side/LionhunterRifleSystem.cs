//

using Content.Shared.Actions;
using Content.Shared.ADT.Heretic.Common;
using Content.Shared.ADT.Heretic.Systems;
using Content.Shared.CombatMode;
using Content.Shared.DoAfter;
using Content.Shared.Examine;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Heretic;
using Content.Shared.Heretic.Components;
using Content.Shared.Popups;
using Content.Shared.Projectiles;
using Content.Shared.Stunnable;
using Content.Shared.Timing;
using Content.Shared.Weapons.Ranged.Components;
using Content.Shared.Weapons.Ranged.Events;
using Content.Shared.Weapons.Ranged.Systems;
using Content.Shared.Whitelist;
using Content.Shared.Wieldable.Components;
using Robust.Shared.Input;
using Robust.Shared.Input.Binding;
using Robust.Shared.Map;
using Robust.Shared.Physics.Events;
using Robust.Shared.Player;
using Robust.Shared.Serialization;

namespace Content.Shared.ADT.Heretic.Systems.Side;

public sealed partial class LionhunterRifleSystem : EntitySystem
{
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly SharedHereticSystem _heretic = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly SharedGunSystem _gun = default!;
    [Dependency] private readonly SharedStunSystem _stun = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly EntityWhitelistSystem _whitelist = default!;
    [Dependency] private readonly SharedCombatModeSystem _combat = default!;
    [Dependency] private readonly UseDelaySystem _delay = default!;

    [Dependency] private readonly EntityQuery<WieldableComponent> _wieldableQuery = default!;
    [Dependency] private readonly EntityQuery<LionhunterRifleProjectileComponent> _lionhunterProjectileQuery = default!;
    [Dependency] private readonly EntityQuery<HomingProjectileComponent> _homingQuery = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<AimedRifleComponent, DoAfterAttemptEvent<AimedRifleDoAfterEvent>>(OnDoAfterAttempt);
        SubscribeLocalEvent<AimedRifleComponent, AimedRifleDoAfterEvent>(OnDoAfter);

        SubscribeLocalEvent<LionhunterRifleComponent, ExaminedEvent>(OnExamine);
        SubscribeLocalEvent<LionhunterRifleComponent, AimedRifleAimAttemptEvent>(OnAimAttempt);
        SubscribeLocalEvent<LionhunterRifleComponent, AmmoShotEvent>(OnShoot);

        SubscribeLocalEvent<LionhunterRifleProjectileComponent, PreventCollideEvent>(OnPreventCollide);
        SubscribeLocalEvent<LionhunterRifleProjectileComponent, ProjectileHitEvent>(OnHit);

        CommandBinds.Builder
            .Bind(EngineKeyFunctions.UseSecondary, new PointerInputCmdHandler(Aim))
            .Register<LionhunterRifleSystem>();
    }

    public override void Shutdown()
    {
        base.Shutdown();

        CommandBinds.Unregister<LionhunterRifleSystem>();
    }

    private bool Aim(ICommonSession? session, EntityCoordinates coords, EntityUid uid)
    {
        if (session?.AttachedEntity is not { Valid: true } player || !Exists(player) || !Exists(uid))
            return false;

        AimRifle(player, uid);
        return false;
    }

    private void OnHit(Entity<LionhunterRifleProjectileComponent> ent, ref ProjectileHitEvent args)
    {
        if (ent.Comp.EmpowerTarget is not { } target || args.Target != target)
            return;

        _stun.TryKnockdown(target, ent.Comp.KnockdownTime, true);

        var path = ent.Comp.ShooterPath ?? "Blade";
        var mark = EnsureComp<HereticCombatMarkComponent>(target);
        mark.DisappearTime = mark.MaxDisappearTime;
        mark.Path = path;
        mark.Repetitions = 1;
        Dirty(target, mark);
    }

    private void OnPreventCollide(Entity<LionhunterRifleProjectileComponent> ent, ref PreventCollideEvent args)
    {
        if (ent.Comp.EmpowerTarget is { } target && args.OtherEntity != target)
            args.Cancelled = true;
    }

    private void OnAimAttempt(Entity<LionhunterRifleComponent> ent, ref AimedRifleAimAttemptEvent args)
    {
        if (args.Cancelled || _heretic.IsHereticOrGhoul(args.User))
            return;

        args.Cancelled = true;
    }

    private void OnExamine(Entity<LionhunterRifleComponent> ent, ref ExaminedEvent args)
    {
        if (!_heretic.IsHereticOrGhoul(args.Examiner))
            return;

        args.PushMarkup(Loc.GetString("lionhunter-rifle-examine-message"));
    }

    private void OnShoot(Entity<LionhunterRifleComponent> ent, ref AmmoShotEvent args)
    {
        if (!TryComp(ent.Owner, out AimedRifleComponent? aim) || aim.AimingAt is not { } target)
            return;

        string? path = null;
        if (aim.AimingUser is { } shooter && _heretic.TryGetHereticComponent(shooter, out var heretic, out _))
            path = heretic.CurrentPath;

        foreach (var uid in args.FiredProjectiles)
        {
            if (!_lionhunterProjectileQuery.TryComp(uid, out var comp))
                continue;

            EntityManager.AddComponents(uid, comp.ComponentsOnEmpower);

            comp.ShooterPath = path;
            comp.ShooterPassiveLevel = 1;
            comp.EmpowerTarget = target;
            Dirty(uid, comp);

            if (_homingQuery.TryComp(uid, out var homing))
            {
                homing.Target = target;
                Dirty(uid, homing);
            }
        }
    }

    private void OnDoAfter(Entity<AimedRifleComponent> ent, ref AimedRifleDoAfterEvent args)
    {
        if (ent.Comp.AimingAt is not { } target)
            return;

        if (args is { Cancelled: false, Handled: false } && Exists(args.Target) && args.Target.Value == target)
        {
            args.Handled = true;

            if (!TryComp(ent, out GunComponent? gun))
                return;

            _gun.AttemptShoot(args.User, (ent, gun), Transform(target).Coordinates, target);
            _delay.TryResetDelay(ent.Owner, id: ent.Comp.AimUseDelayId);
        }

        if (ent.Comp.ShowMark)
            RemCompDeferred<AimedRifleMarkerComponent>(target);
        ent.Comp.AimingAt = null;
        ent.Comp.AimingUser = null;
        Dirty(ent);
    }

    private void OnDoAfterAttempt(Entity<AimedRifleComponent> ent,
        ref DoAfterAttemptEvent<AimedRifleDoAfterEvent> args)
    {
        if (ent.Comp.AimingAt != args.Event.Target ||
            _wieldableQuery.TryComp(ent, out var wieldable) && !wieldable.Wielded ||
            args.Event.Target is not { } target || !_transform.InRange(args.Event.User, target, ent.Comp.MaxDistance))
            args.Cancel();
    }

    private void AimRifle(EntityUid user, EntityUid target)
    {
        if (target == user || !_combat.IsInCombatMode(user) || !TryComp(user, out DoAfterComponent? doAfter))
            return;

        if (!_hands.TryGetActiveItem(user, out var ent) || !TryComp(ent.Value, out AimedRifleComponent? comp) ||
            _delay.IsDelayed(ent.Value, comp.AimUseDelayId))
            return;

        if (_whitelist.IsWhitelistFail(comp.AimWhitelist, target))
            return;

        var ev = new AimedRifleAimAttemptEvent(ent.Value, user, target);
        RaiseLocalEvent(ent.Value, ref ev);
        if (ev.Cancelled)
            return;

        if (_wieldableQuery.TryComp(ent.Value, out var wieldable) && !wieldable.Wielded)
        {
            _popup.PopupEntity(Loc.GetString("wieldable-component-requires", ("item", ent.Value)), user, user);
            return;
        }

        var coords = Transform(user).Coordinates;
        var otherCoords = Transform(target).Coordinates;
        if (!coords.TryDistance(EntityManager, _transform, otherCoords, out var distance) ||
            distance > comp.MaxDistance)
            return;

        if (distance < comp.MinDistance)
        {
            _popup.PopupEntity(Loc.GetString("heretic-ability-fail-too-close"), user, user);
            return;
        }

        var time = comp.AimTimePerDistance * distance;
        if (time > comp.MaxAimTime)
            time = comp.MaxAimTime;

        var doArgs = new DoAfterArgs(EntityManager,
            user,
            time,
            new AimedRifleDoAfterEvent(),
            ent.Value,
            target,
            ent.Value)
        {
            MultiplyDelay = false,
            AttemptFrequency = AttemptFrequency.EveryTick,
            BreakOnDropItem = true,
            BreakOnHandChange = true,
            NeedHand = true,
            RequireCanInteract = false,
            DistanceThreshold = null,
            DuplicateCondition = DuplicateConditions.SameTarget,
        };

        var dirtied = false;

        if (comp.AimingAt != target)
        {
            if (comp.ShowMark && Exists(comp.AimingAt))
                RemCompDeferred<AimedRifleMarkerComponent>(comp.AimingAt.Value);

            foreach (var (id, da) in doAfter.DoAfters)
            {
                if (da.Cancelled || da.Completed)
                    continue;

                if (da.AttemptEvent?.GetType() != typeof(DoAfterAttemptEvent<AimedRifleDoAfterEvent>))
                    continue;

                _doAfter.Cancel(user, id, doAfter, true);
            }

            comp.AimingAt = target;
            comp.AimingUser = user;
            Dirty(ent.Value, comp);
            dirtied = true;
        }

        if (_doAfter.TryStartDoAfter(doArgs))
        {
            _popup.PopupEntity(Loc.GetString("lionhunter-rifle-aim-message"), user, user);
            if (comp.ShowMark)
                EnsureComp<AimedRifleMarkerComponent>(target);
            return;
        }

        comp.AimingAt = null;
        comp.AimingUser = null;
        if (!dirtied)
            Dirty(ent.Value, comp);
    }

    [Serializable, NetSerializable]
    private sealed partial class AimedRifleDoAfterEvent : SimpleDoAfterEvent;
}
