using Content.Shared.Weapons.Melee;
using Content.Shared.Weapons.Melee.Events;
using Content.Shared.Weapons.Ranged.Events;
using Content.Shared.Weapons.Ranged.Systems;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Network;
using Robust.Shared.Timing;

namespace Content.Shared.ADT.Weapons.KineticCooldown;

public sealed class ADTKineticCooldownSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly SharedMeleeWeaponSystem _melee = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ADTKineticCooldownComponent, ShotAttemptedEvent>(OnShotAttempted);
        SubscribeLocalEvent<ADTKineticCooldownComponent, GunShotEvent>(OnGunShot);
        SubscribeLocalEvent<ADTKineticCooldownComponent, AttemptMeleeEvent>(OnAttemptMelee);
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

            if (comp.RechargeSound == null)
                continue;

            if (_net.IsServer)
                _audio.PlayPvs(comp.RechargeSound, uid);
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

        SetCooldown(ent, _timing.CurTime + recovery.Value * ent.Comp.MeleeMultiplier, false);
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
        var tolerance = _net.IsServer ? ent.Comp.PredictionTolerance : TimeSpan.Zero;

        return _timing.CurTime + tolerance < ent.Comp.NextUse;
    }

    public void SetCooldown(Entity<ADTKineticCooldownComponent> ent, TimeSpan next, bool playRechargeSound = true)
    {
        if (next <= ent.Comp.NextUse)
            return;

        ent.Comp.LastUseStart = _timing.CurTime;
        ent.Comp.NextUse = next;
        ent.Comp.RechargePending = playRechargeSound;
        Dirty(ent);
    }
}
