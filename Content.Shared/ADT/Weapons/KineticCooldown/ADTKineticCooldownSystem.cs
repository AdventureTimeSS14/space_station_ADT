using Content.Shared.Examine;
using Content.Shared.Weapons.Melee;
using Content.Shared.Weapons.Melee.Events;
using Content.Shared.Weapons.Ranged.Components;
using Content.Shared.Weapons.Ranged.Events;
using Content.Shared.Weapons.Ranged.Systems;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Timing;

namespace Content.Shared.ADT.Weapons.KineticCooldown;

public sealed class ADTKineticCooldownSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedGunSystem _gun = default!;
    [Dependency] private readonly SharedMeleeWeaponSystem _melee = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ADTKineticCooldownComponent, ShotAttemptedEvent>(OnShotAttempted);
        SubscribeLocalEvent<ADTKineticCooldownComponent, GunShotEvent>(OnGunShot);
        SubscribeLocalEvent<ADTKineticCooldownComponent, AttemptMeleeEvent>(OnAttemptMelee);
        SubscribeLocalEvent<ADTKineticCooldownComponent, ExaminedEvent>(OnExamined);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var curTime = _timing.CurTime;
        var query = EntityQueryEnumerator<ADTKineticCooldownComponent>();

        while (query.MoveNext(out var uid, out var comp))
        {
            if (!comp.RechargePending || curTime < comp.NextUse)
                continue;

            comp.RechargePending = false;
            Dirty(uid, comp);

            _audio.PlayLocal(comp.RechargeSound, uid, null);
        }
    }

    private void OnShotAttempted(Entity<ADTKineticCooldownComponent> ent, ref ShotAttemptedEvent args)
    {
        if (args.Cancelled || !IsDelayed(ent))
            return;

        args.Cancel();
    }

    private void OnGunShot(Entity<ADTKineticCooldownComponent> ent, ref GunShotEvent args)
    {
        if (TryComp<GunComponent>(ent, out var gun) && gun.BurstActivated)
            return;

        var recovery = GetSwingTime(ent, args.User);

        if (recovery == null)
            return;

        SetCooldown(ent, _timing.CurTime + recovery.Value * ent.Comp.CurrentRangedMultiplier);
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

        SetCooldown(ent, _timing.CurTime + recovery.Value * ent.Comp.CurrentMeleeMultiplier, false);
    }

    private void OnExamined(Entity<ADTKineticCooldownComponent> ent, ref ExaminedEvent args)
    {
        if (!args.IsInDetailsRange)
            return;

        var swing = GetSwingTime(ent, args.Examiner);

        if (swing == null)
            return;

        using (args.PushGroup(nameof(ADTKineticCooldownComponent)))
        {
            if (TryComp<GunComponent>(ent, out var gun))
            {
                args.PushMarkup(Loc.GetString("adt-kinetic-cooldown-examine-mode",
                    ("mode", Loc.GetString($"gun-{gun.SelectedMode}"))));

                var ranged = swing.Value * ent.Comp.CurrentRangedMultiplier;
                args.PushMarkup(Loc.GetString("adt-kinetic-cooldown-examine-ranged",
                    ("seconds", $"{ranged.TotalSeconds:0.0#}")));
            }

            var melee = swing.Value * ent.Comp.CurrentMeleeMultiplier;
            args.PushMarkup(Loc.GetString("adt-kinetic-cooldown-examine-melee",
                ("seconds", $"{melee.TotalSeconds:0.0#}")));
        }
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

    public void SetCooldown(Entity<ADTKineticCooldownComponent> ent, TimeSpan next, bool playRechargeSound = true)
    {
        if (next <= ent.Comp.NextUse)
            return;

        ent.Comp.LastUseStart = _timing.CurTime;
        ent.Comp.NextUse = next;
        ent.Comp.RechargePending = playRechargeSound;
        Dirty(ent);

        _gun.ADTSetNextFire(ent.Owner, ent.Comp.NextUse);
    }

    public void ReduceCooldown(Entity<ADTKineticCooldownComponent> ent, TimeSpan next)
    {
        if (next >= ent.Comp.NextUse)
            return;

        ent.Comp.NextUse = next;
        Dirty(ent);

        _gun.ADTSetNextFire(ent.Owner, ent.Comp.NextUse);
    }
}
