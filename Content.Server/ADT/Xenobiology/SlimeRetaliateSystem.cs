using Content.Server.ADT.Xenobiology.Systems;
using Content.Server.NPC;
using Content.Server.NPC.Components;
using Content.Server.NPC.HTN;
using Content.Server.Stunnable;
using Content.Shared.ADT.Xenobiology.Components;
using Content.Shared.Damage;
using Content.Shared.Damage.Systems;
using Content.Shared.FixedPoint;
using Content.Shared.Mobs.Systems;
using Content.Shared.NPC.Components;
using Content.Shared.NPC.Systems;
using Content.Shared.Weapons.Melee.Events;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server.ADT.Xenobiology;

public sealed partial class SlimeRetaliateSystem : EntitySystem
{
    [Dependency] private readonly SlimeLatchSystem _slimeLatch = default!;
    [Dependency] private readonly StunSystem _stun = default!;
    [Dependency] private readonly HTNSystem _htn = default!;
    [Dependency] private readonly NpcFactionSystem _factions = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SlimeComponent, DamageDealtEvent>(OnDamageDealt);
        SubscribeLocalEvent<SlimeComponent, MeleeHitEvent>(OnMeleeHit);
    }

    private void OnDamageDealt(Entity<SlimeComponent> ent, ref DamageDealtEvent args)
    {
        if (args.Damage.GetTotal() <= FixedPoint2.Zero)
            return;

        if (args.Origin is not { } attacker || attacker == ent.Owner || Deleted(attacker))
            return;

        if (IsFriend(ent, attacker))
            BetrayFriend(ent, attacker);

        if (_slimeLatch.IsLatched(ent))
            _slimeLatch.Unlatch(ent);

        if (!TryComp<SlimeRetaliateComponent>(ent, out var retaliate))
        {
            retaliate = EnsureComp<SlimeRetaliateComponent>(ent);
            if (TryComp<HTNComponent>(ent, out var htn))
                _htn.Replan(htn);
        }

        retaliate.Attacker = attacker;
        retaliate.ExpiresAt = _timing.CurTime + ent.Comp.RetaliateDuration;
        retaliate.MaxDistance = ent.Comp.RetaliateMaxDistance;
    }

    private void OnMeleeHit(Entity<SlimeComponent> ent, ref MeleeHitEvent args)
    {
        if (args.User != ent.Owner || !args.IsHit)
            return;

        if (TryComp<MobGrowthComponent>(ent, out var growth) && growth.IsFirstStage)
            return;

        if (!_random.Prob(ent.Comp.AdultKnockdownChance))
            return;

        foreach (var hit in args.HitEntities)
        {
            if (Deleted(hit) || _mobState.IsDead(hit))
                continue;

            _stun.TryKnockdown(hit, ent.Comp.AdultKnockdownDuration, refresh: true, autoStand: true, drop: true, force: true);
            _stun.TryAddStunDuration(hit, ent.Comp.AdultKnockdownDuration);
        }
    }

    private bool IsFriend(Entity<SlimeComponent> slime, EntityUid other)
    {
        return slime.Comp.Tamer == other
            || _factions.IsIgnored(new Entity<FactionExceptionComponent?>(slime, default), other);
    }

    private void BetrayFriend(Entity<SlimeComponent> slime, EntityUid betrayer)
    {
        if (slime.Comp.Tamer == betrayer)
            slime.Comp.Tamer = null;

        slime.Comp.Friendship = 0f;

        if (slime.Comp.FollowingTarget == betrayer)
            StopFollowing(slime);

        if (TryComp<FactionExceptionComponent>(slime, out var exception))
        {
            exception.Ignored.Remove(betrayer);
            if (TryComp<FactionExceptionTrackerComponent>(betrayer, out var tracker))
                tracker.Entities.Remove(slime);
        }
    }

    private void StopFollowing(Entity<SlimeComponent> slime)
    {
        slime.Comp.FollowingTarget = null;
        RemCompDeferred<SlimeFollowingComponent>(slime);
        if (TryComp<HTNComponent>(slime, out var htn))
            htn.Blackboard.Remove<EntityCoordinates>(NPCBlackboard.FollowTarget);
    }
}