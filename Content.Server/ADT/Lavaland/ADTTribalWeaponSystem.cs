using Content.Shared.ADT.Lavaland.Components;
using Content.Shared.Damage.Systems;
using Content.Shared.IdentityManagement;
using Content.Shared.Mobs.Components;
using Content.Shared.Popups;
using Content.Shared.Silicons.Borgs.Components;
using Content.Shared.Stunnable;
using Content.Shared.Weapons.Melee.Events;
using Content.Shared.Wieldable.Components;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server.ADT.Lavaland;

public sealed class ADTTribalWeaponSystem : EntitySystem
{
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedStunSystem _stun = default!;

    private static readonly SoundSpecifier BurstSound = new SoundCollectionSpecifier("gib");

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ADTBloodlettingOnHitComponent, MeleeHitEvent>(OnBloodlettingHit);
        SubscribeLocalEvent<ADTHitStunChanceComponent, MeleeHitEvent>(OnStunChanceHit);
    }

    private void OnBloodlettingHit(Entity<ADTBloodlettingOnHitComponent> ent, ref MeleeHitEvent args)
    {
        if (!args.IsHit || args.HitEntities.Count == 0)
            return;

        if (ent.Comp.RequireWielded && (!TryComp<WieldableComponent>(ent.Owner, out var wieldable) || !wieldable.Wielded))
            return;

        foreach (var target in args.HitEntities)
        {
            if (target == args.User || !HasComp<MobStateComponent>(target))
                continue;

            AddBloodletting(target, ent.Comp);
        }
    }

    private void OnStunChanceHit(Entity<ADTHitStunChanceComponent> ent, ref MeleeHitEvent args)
    {
        if (!args.IsHit || args.HitEntities.Count == 0)
            return;

        foreach (var target in args.HitEntities)
        {
            if (target == args.User || !HasComp<MobStateComponent>(target))
                continue;

            if (HasComp<BorgChassisComponent>(target))
            {
                if (_random.Prob(ent.Comp.SiliconStunChance))
                    _stun.TryAddStunDuration(target, ent.Comp.SiliconStunTime);

                continue;
            }

            if (_random.Prob(ent.Comp.KnockdownChance))
                _stun.TryKnockdown(target, ent.Comp.KnockdownTime);
        }
    }

    private void AddBloodletting(EntityUid target, ADTBloodlettingOnHitComponent source)
    {
        var bleed = EnsureComp<ADTBloodlettingComponent>(target);
        bleed.Cap = source.Cap;
        bleed.Damage = source.Damage;
        bleed.DecayInterval = source.DecayInterval;
        bleed.Stacks += source.Stacks;

        if (bleed.Stacks < bleed.Cap)
        {
            bleed.NextDecay = _timing.CurTime + bleed.DecayInterval + source.DelayPerHit;
            return;
        }

        RemComp<ADTBloodlettingComponent>(target);
        _damageable.TryChangeDamage(target, source.Damage, true);
        _audio.PlayPvs(BurstSound, target);
        _popup.PopupEntity(Loc.GetString("adt-bloodletting-burst", ("target", Identity.Entity(target, EntityManager))), target, PopupType.MediumCaution);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var curTime = _timing.CurTime;
        var query = EntityQueryEnumerator<ADTBloodlettingComponent>();
        while (query.MoveNext(out var uid, out var bleed))
        {
            if (bleed.NextDecay > curTime)
                continue;

            bleed.Stacks--;
            bleed.NextDecay = curTime + bleed.DecayInterval;

            if (bleed.Stacks <= 0)
                RemCompDeferred<ADTBloodlettingComponent>(uid);
        }
    }
}
