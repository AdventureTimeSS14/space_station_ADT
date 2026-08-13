using Content.Shared.Weapons.Melee;
using Content.Shared.Weapons.Melee.Events;
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
        var recovery = GetSwingTime(ent, args.User);
        if (recovery == null)
            return;

        SetCooldown(ent, _timing.CurTime + recovery.Value * ent.Comp.RangedMultiplier);
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

        var recovery = GetSwingTime(ent, args.User);

        if (recovery == null)
            return;

        SetCooldown(ent, _timing.CurTime + recovery.Value * ent.Comp.MeleeMultiplier);
    }

    private TimeSpan? GetSwingTime(Entity<ADTKineticCooldownComponent> ent, EntityUid user)
    {
        if (!HasComp<MeleeWeaponComponent>(ent))
            return null;

        var rate = _melee.GetAttackRate(ent, user);

        if (rate <= 0f)
            return null;

        return TimeSpan.FromSeconds(1f / rate);
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
