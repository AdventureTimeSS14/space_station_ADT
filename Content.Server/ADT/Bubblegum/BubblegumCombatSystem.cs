using Content.Server.ADT.Bubblegum.Abilities;
using Content.Server.NPC.HTN;
using Content.Server.NPC.Systems;
using Content.Shared.ADT.Bubblegum;
using Content.Shared.ADT.Bubblegum.Abilities;
using Content.Shared.Mobs.Systems;
using Robust.Shared.Map;
using Robust.Shared.Player;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server.ADT.Bubblegum;

public sealed class BubblegumCombatSystem : EntitySystem
{
    [Dependency] private readonly BubblegumSystem _bubblegum = default!;
    [Dependency] private readonly BubblegumBloodWarpSystem _warp = default!;
    [Dependency] private readonly BubblegumHallucinationChargeSystem _hallucination = default!;
    [Dependency] private readonly BubblegumSummonNarsiSystem _narsi = default!;
    [Dependency] private readonly BubblegumSurroundSystem _surround = default!;
    [Dependency] private readonly BubblegumTripleChargeSystem _tripleCharge = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly NPCSystem _npc = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BubblegumPendingChargeComponent, ComponentInit>(OnBusyAdded);
        SubscribeLocalEvent<BubblegumPendingChargeComponent, ComponentShutdown>(OnBusyRemoved);
        SubscribeLocalEvent<BubblegumActiveChargeComponent, ComponentInit>(OnBusyAdded);
        SubscribeLocalEvent<BubblegumPendingWavesComponent, ComponentInit>(OnBusyAdded);
        SubscribeLocalEvent<BubblegumPendingWavesComponent, ComponentShutdown>(OnBusyRemoved);
    }

    private void OnBusyAdded<T>(EntityUid uid, T comp, ComponentInit args) where T : IComponent
    {
        if (!TryComp<HTNComponent>(uid, out var htn))
            return;

        htn.Blackboard.SetValue("BubblegumWavesBusy", true);
    }

    private void OnBusyRemoved<T>(EntityUid uid, T comp, ComponentShutdown args) where T : IComponent
    {
        if (HasComp<BubblegumPendingChargeComponent>(uid)
            || HasComp<BubblegumActiveChargeComponent>(uid)
            || HasComp<BubblegumPendingWavesComponent>(uid))
            return;

        RemoveBubblegumBusy(uid);
    }

    public void RemoveBubblegumBusy(EntityUid uid)
    {
        if (!TryComp<HTNComponent>(uid, out var htn))
            return;

        htn.Blackboard.Remove<bool>("BubblegumWavesBusy");
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var now = _timing.CurTime;
        var query = EntityQueryEnumerator<BubblegumComponent, HTNComponent>();
        while (query.MoveNext(out var uid, out var comp, out var htn))
        {
            if (!_npc.IsAwake(uid, htn))
                continue;

            if (HasComp<ActorComponent>(uid))
                continue;

            if (_mobState.IsDead(uid))
                continue;

            if (now < comp.NextAbilityAt)
                continue;

            if (IsBusy(uid))
                continue;

            if (!htn.Blackboard.TryGetValue<EntityUid>("Target", out var target, EntityManager)
                || TerminatingOrDeleted(target))
                continue;

            comp.NextAbilityAt = now + comp.AbilityInterval;
            Decide((uid, comp), target);
        }
    }

    private bool IsBusy(EntityUid uid)
    {
        return HasComp<BubblegumPendingChargeComponent>(uid)
               || HasComp<BubblegumActiveChargeComponent>(uid)
               || HasComp<BubblegumPendingWavesComponent>(uid)
               || HasComp<BubblegumPendingWarpComponent>(uid);
    }

    private void Decide(Entity<BubblegumComponent> ent, EntityUid target)
    {
        var targetCoords = _transform.GetMapCoordinates(target);
        if (targetCoords.MapId == MapId.Nullspace)
            return;

        // anger_modifier (0..20)
        var anger = ent.Comp.AngerModifier;

        // if(!try_bloodattack() || prob(25 + anger)) blood_warp()
        if (_random.Prob(Math.Clamp(0.25f + anger / 100f, 0f, 1f))
            && TryComp<BubblegumBloodWarpComponent>(ent, out var warp)
            && _warp.TryBloodWarp((ent.Owner, warp), targetCoords))
        {
            _bubblegum.Say(ent, "bubblegum-blood-warp", 3);
            return;
        }

        // if(!BUBBLEGUM_SMASH) triple_charge()
        if (!ent.Comp.InSmashPhase)
        {
            if (TryComp<BubblegumTripleChargeComponent>(ent, out var tc))
            {
                _bubblegum.Say(ent, "bubblegum-triple-charge", 3);
                _tripleCharge.StartNpcTripleCharge((ent.Owner, tc), targetCoords);
            }
            return;
        }

        // prob(25) && enraged → hit_up_narsi()
        if (_bubblegum.IsEnraged(ent) && _random.Prob(0.25f))
        {
            if (TryComp<BubblegumSummonNarsiComponent>(ent, out var narsi))
            {
                _bubblegum.Say(ent, "bubblegum-narsi", 3);
                _narsi.TrySummon((ent.Owner, narsi), targetCoords);
            }
            return;
        }

        if (_random.Prob(Math.Clamp(0.5f + anger / 100f, 0f, 1f)))
        {
            if (TryComp<BubblegumHallucinationChargeComponent>(ent, out var hc))
            {
                _bubblegum.Say(ent, "bubblegum-hallucination", 3);
                _hallucination.StartHallucinationCharge((ent.Owner, hc), targetCoords, target);
            }
            return;
        }

        if (TryComp<BubblegumSurroundComponent>(ent, out var sc))
        {
            _bubblegum.Say(ent, "bubblegum-surround", 3);
            _surround.StartSurround((ent.Owner, sc), targetCoords, target);
        }
    }

}
