using Content.Server.ADT.Achievements.Components;
using Content.Shared.ADT.Achievements;
using Content.Shared.Damage;
using Content.Shared.Damage.Systems;
using Content.Shared.Destructible;
using Content.Shared.Mind;
using Content.Shared.Mobs;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;

namespace Content.Server.ADT.Achievements;

public sealed partial class ADTAchievementSystem
{
    [Dependency] private readonly SharedMindSystem _mind = default!;

    private void InitializeHooks()
    {
        SubscribeLocalEvent<ADTAchievementSourceComponent, MobStateChangedEvent>(OnSourceMobStateChanged);
        SubscribeLocalEvent<ADTAchievementSourceComponent, DamageChangedEvent>(OnSourceDamaged);
        SubscribeLocalEvent<ADTAchievementSourceComponent, DestructionEventArgs>(OnSourceDestroyed);
    }

    private void OnSourceMobStateChanged(Entity<ADTAchievementSourceComponent> ent, ref MobStateChangedEvent args)
    {
        if (args.NewMobState != MobState.Dead || ent.Comp.OnDeath.Count == 0)
            return;

        Credit(args.Origin ?? ent.Comp.LastAttacker, ent.Comp.OnDeath, ent.Owner);
    }

    private void OnSourceDamaged(Entity<ADTAchievementSourceComponent> ent, ref DamageChangedEvent args)
    {
        if (!args.DamageIncreased || args.Origin is not { } origin)
            return;

        ent.Comp.LastAttacker = origin;
    }

    private void OnSourceDestroyed(Entity<ADTAchievementSourceComponent> ent, ref DestructionEventArgs args)
    {
        if (ent.Comp.OnDestroyed.Count == 0)
            return;

        Credit(ent.Comp.LastAttacker, ent.Comp.OnDestroyed, ent.Owner);
    }

    public void OnGathered(EntityUid gathered, EntityUid? gatherer)
    {
        if (!TryComp<ADTAchievementSourceComponent>(gathered, out var source) || source.OnGathered.Count == 0)
            return;

        Credit(gatherer, source.OnGathered, gathered);
    }

    public void OnHarvested(EntityUid harvested, EntityUid harvester, int amount)
    {
        if (!TryComp<ADTAchievementSourceComponent>(harvested, out var source) || source.OnHarvested.Count == 0)
            return;

        Credit(harvester, source.OnHarvested, harvested, amount);
    }

    private void Credit(
        EntityUid? actor,
        List<ProtoId<ADTAchievementTriggerPrototype>> triggers,
        EntityUid? target,
        int amount = 1)
    {
        if (GetUser(actor) is not { } user)
            return;

        foreach (var trigger in triggers)
        {
            Raise(user, trigger, target, amount: amount);
        }
    }

    private NetUserId? GetUser(EntityUid? actor)
    {
        if (actor is not { } uid || !_mind.TryGetMind(uid, out _, out var mind))
            return null;

        return mind.UserId;
    }
}
