using Content.Shared.ADT.Crushers.Components;
using Content.Shared.Examine;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Popups;
using Content.Shared.Projectiles;
using Content.Shared.StatusEffectNew;
using Content.Shared.Weapons.Melee.Events;
using Content.Shared.Weapons.Ranged.Systems;
using Robust.Shared.Network;
using Robust.Shared.Timing;

namespace Content.Shared.ADT.Crushers.Systems;

public sealed class ADTKineticFocusSystem : EntitySystem
{
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private INetManager _net = default!;
    [Dependency] private MobStateSystem _mobState = default!;
    [Dependency] private StatusEffectsSystem _status = default!;
    [Dependency] private SharedPopupSystem _popup = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ADTKineticFocusWeaponComponent, MeleeHitEvent>(OnMeleeHit);
        SubscribeLocalEvent<ADTKineticFocusWeaponComponent, GunShotEvent>(OnGunShot);
        SubscribeLocalEvent<ADTKineticFocusWeaponComponent, ExaminedEvent>(OnExamined);
        SubscribeLocalEvent<ADTKineticFocusProjectileComponent, ProjectileHitEvent>(OnProjectileHit);
    }

    public float GetCooldownMultiplier(EntityUid user)
    {
        if (!_status.TryEffectsWithComp<ADTKineticInterferenceStatusEffectComponent>(user, out var effects))
            return 1f;

        var multiplier = 1f;

        foreach (var effect in effects)
        {
            multiplier *= effect.Comp1.CooldownMultiplier;
        }

        return multiplier;
    }

    private void OnMeleeHit(Entity<ADTKineticFocusWeaponComponent> ent, ref MeleeHitEvent args)
    {
        if (!ent.Comp.TrackMelee || !args.IsHit)
            return;

        foreach (var target in args.HitEntities)
        {
            if (!IsValidTarget(args.User, target))
                continue;

            RegisterHit(ent, args.User);
            return;
        }
    }

    private void OnGunShot(Entity<ADTKineticFocusWeaponComponent> ent, ref GunShotEvent args)
    {
        if (_net.IsClient || !ent.Comp.TrackRanged)
            return;

        foreach (var (ammo, _) in args.Ammo)
        {
            if (ammo is { } projectile && !TerminatingOrDeleted(projectile))
                EnsureComp<ADTKineticFocusProjectileComponent>(projectile);
        }
    }

    private void OnProjectileHit(Entity<ADTKineticFocusProjectileComponent> ent, ref ProjectileHitEvent args)
    {
        if (args.Shooter is not { } shooter ||
            !TryComp<ProjectileComponent>(ent, out var projectile) ||
            projectile.Weapon is not { } weapon ||
            !TryComp<ADTKineticFocusWeaponComponent>(weapon, out var focusWeapon) ||
            !IsValidTarget(shooter, args.Target))
        {
            return;
        }

        RegisterHit((weapon, focusWeapon), shooter);
    }

    private void OnExamined(Entity<ADTKineticFocusWeaponComponent> ent, ref ExaminedEvent args)
    {
        if (!args.IsInDetailsRange)
            return;

        args.PushMarkup(Loc.GetString("adt-kinetic-focus-examine",
            ("window", ent.Comp.Window.TotalSeconds.ToString("0.#"))));
    }

    private bool IsValidTarget(EntityUid user, EntityUid target)
    {
        return target != user &&
               HasComp<MobStateComponent>(target) &&
               !_mobState.IsDead(target);
    }

    private void RegisterHit(Entity<ADTKineticFocusWeaponComponent> weapon, EntityUid user)
    {
        if (_net.IsClient)
            return;

        var now = _timing.CurTime;
        var focus = EnsureComp<ADTKineticFocusComponent>(user);

        var mixing = focus.LastHit != TimeSpan.Zero &&
                     focus.Discipline != weapon.Comp.Discipline &&
                     now - focus.LastHit <= weapon.Comp.Window;

        focus.Discipline = weapon.Comp.Discipline;
        focus.LastHit = now;

        if (!mixing)
            return;

        var hadEffect = _status.HasStatusEffect(user, weapon.Comp.Effect);

        if (!_status.TrySetStatusEffectDuration(user, weapon.Comp.Effect, weapon.Comp.EffectDuration) || hadEffect)
            return;

        _popup.PopupEntity(Loc.GetString("adt-kinetic-interference-popup"), user, user, PopupType.MediumCaution);
    }
}
