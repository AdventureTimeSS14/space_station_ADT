using Content.Shared.Weapons.Melee;
using Content.Shared.Weapons.Melee.Events;
using Content.Shared.Weapons.Ranged.Components;
using Content.Shared.Weapons.Ranged.Systems;
using Robust.Shared.Timing;

namespace Content.Shared.ADT.Weapons.KineticCooldown;

public sealed class ADTKineticCooldownSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedMeleeWeaponSystem _melee = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ADTKineticCooldownComponent, AttemptShootEvent>(OnAttemptShoot);
        SubscribeLocalEvent<ADTKineticCooldownComponent, GunShotEvent>(OnGunShot);
        SubscribeLocalEvent<ADTKineticCooldownComponent, AttemptMeleeEvent>(OnAttemptMelee);
    }

    private void OnAttemptShoot(Entity<ADTKineticCooldownComponent> ent, ref AttemptShootEvent args)
    {
        if (args.Cancelled || !IsDelayed(ent))
            return;

        args.Cancelled = true;
    }

    private void OnGunShot(Entity<ADTKineticCooldownComponent> ent, ref GunShotEvent args)
    {
        if (!TryComp<GunComponent>(ent, out var gun))
            return;

        var now = _timing.CurTime;
        var recovery = gun.NextFire - now;
        if (recovery < TimeSpan.Zero)
            recovery = TimeSpan.Zero;

        SetCooldown(ent, now + recovery * ent.Comp.RangedMultiplier);
    }

    private void OnAttemptMelee(Entity<ADTKineticCooldownComponent> ent, ref AttemptMeleeEvent args)
    {
        if (args.Cancelled)
            return;

        if (IsDelayed(ent))
        {
            args.Cancelled = true;
            return;
        }

        var rate = _melee.GetAttackRate(ent, args.User);
        if (rate <= 0f)
            return;

        SetCooldown(ent, _timing.CurTime + TimeSpan.FromSeconds(1f / rate) * ent.Comp.MeleeMultiplier);
    }

    public bool IsDelayed(Entity<ADTKineticCooldownComponent> ent)
    {
        return _timing.CurTime < ent.Comp.NextUse;
    }

    public void SetCooldown(Entity<ADTKineticCooldownComponent> ent, TimeSpan next)
    {
        if (next <= ent.Comp.NextUse)
            return;

        ent.Comp.LastUseStart = _timing.CurTime;
        ent.Comp.NextUse = next;
        Dirty(ent);
    }
}
