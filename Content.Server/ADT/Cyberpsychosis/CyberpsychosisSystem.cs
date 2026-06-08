using Content.Server.ADT.Hallucinations;
using Content.Server.ForceAttack;
using Content.Shared.ADT.Cyberpsychosis;
using Content.Shared.ADT.Traits.Assorted;
using Content.Shared.Implants;
using Content.Shared.Implants.Components;
using Content.Shared.NPC.Systems;
using Content.Shared.StatusEffect;
using Content.Shared.Stunnable;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server.ADT.Cyberpsychosis;

public sealed class CyberpsychosisSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly HallucinationsSystem _hallucinations = default!;
    [Dependency] private readonly StatusEffectsSystem _status = default!;
    [Dependency] private readonly SharedStunSystem _stun = default!;
    [Dependency] private readonly NpcFactionSystem _faction = default!;

    private const string HallucinationsKey = "ADTHallucinations";
    private const string RageFaction = "SimpleHostile";
    private static readonly TimeSpan StressTickInterval = TimeSpan.FromSeconds(1);

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<SubdermalImplantComponent, ImplantImplantedEvent>(OnImplantAdded);
        SubscribeLocalEvent<SubdermalImplantComponent, ImplantRemovedEvent>(OnImplantRemoved);
        SubscribeLocalEvent<CyberpsychosisComponent, MapInitEvent>(OnCyberpsychosisMapInit);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);
        var query = EntityQueryEnumerator<CyberpsychosisComponent>();
        while (query.MoveNext(out var uid, out var comp))
            UpdatePsychosis(uid, comp);
    }

    private void OnCyberpsychosisMapInit(EntityUid uid, CyberpsychosisComponent comp, MapInitEvent args)
    {
        UpdateState(uid, comp);
    }

    private void OnImplantAdded(EntityUid uid, SubdermalImplantComponent implantComp, ref ImplantImplantedEvent args)
    {
        if (!TryComp<CyberneticLoadComponent>(uid, out var cyberImplant))
            return;

        if (!TryComp<CyberpsychosisComponent>(args.Implanted, out var psychosis))
            return;

        psychosis.CurrentLoad += cyberImplant.ImplantLoad;
        UpdateState(args.Implanted, psychosis);
    }

    private void OnImplantRemoved(EntityUid uid, SubdermalImplantComponent implantComp, ref ImplantRemovedEvent args)
    {
        if (!TryComp<CyberneticLoadComponent>(uid, out var cyberImplant))
            return;

        if (!TryComp<CyberpsychosisComponent>(args.Implanted, out var psychosis))
            return;

        psychosis.CurrentLoad = Math.Max(0, psychosis.CurrentLoad - cyberImplant.ImplantLoad);
        UpdateState(args.Implanted, psychosis);
    }

    private void UpdateState(EntityUid uid, CyberpsychosisComponent comp)
    {
        var newState = CalcState(comp);
        if (newState == comp.CurrentState)
            return;

        var oldState = comp.CurrentState;
        comp.CurrentState = newState;

        if (oldState == CyberpsychosisState.None && newState != CyberpsychosisState.None)
            RollIncrement(comp);

        RaiseLocalEvent(uid, new CyberpsychosisStateChangedEvent(uid, oldState, newState));
    }

    private static CyberpsychosisState CalcState(CyberpsychosisComponent comp)
    {
        var overflow = comp.CurrentLoad - comp.MaxLoad;
        return overflow switch
        {
            <= 0 => CyberpsychosisState.None,
            < 30 => CyberpsychosisState.Mild,
            < 70 => CyberpsychosisState.Moderate,
            _ => CyberpsychosisState.Severe
        };
    }

    private void UpdatePsychosis(EntityUid uid, CyberpsychosisComponent comp)
    {
        var now = _timing.CurTime;

        if (comp.InEpisode)
        {
            if (now >= comp.EpisodeEnd)
                EndEpisode(uid, comp);
            return;
        }

        if (comp.CurrentState == CyberpsychosisState.None)
            return;

        if (now < comp.NextStressTick)
            return;

        comp.NextStressTick = now + StressTickInterval;
        comp.StressLevel += comp.StressIncrement;

        if (comp.StressLevel >= comp.StressThreshold)
        {
            comp.StressLevel = 0f;
            var overflow = comp.CurrentLoad - comp.MaxLoad;
            var duration = TimeSpan.FromSeconds(comp.MinDuration + overflow * comp.DurationPerOverflowUnit);
            StartEpisode(uid, comp, duration);
        }
    }

    private void StartEpisode(EntityUid uid, CyberpsychosisComponent comp, TimeSpan duration)
    {
        comp.InEpisode = true;
        comp.EpisodeEnd = _timing.CurTime + duration;

        var active = EnsureComp<ActiveCyberpsychosisComponent>(uid);
        active.State = comp.CurrentState;
        RaiseLocalEvent(uid, new CyberpsychosisEpisodeStartedEvent(comp.CurrentState, duration));

        switch (comp.CurrentState)
        {
            case CyberpsychosisState.Mild:
                ApplyMildEffects(uid, duration, comp);
                break;
            case CyberpsychosisState.Moderate:
                ApplyModerateEffects(uid, duration, comp);
                break;
            case CyberpsychosisState.Severe:
                ApplySevereEffects(uid, duration, comp);
                break;
        }
    }

    private void EndEpisode(EntityUid uid, CyberpsychosisComponent comp)
    {
        comp.InEpisode = false;
        RemComp<ActiveCyberpsychosisComponent>(uid);
        RemoveAllEffects(uid);
        TryRollParalyze(uid, comp);
        RaiseLocalEvent(uid, new CyberpsychosisEpisodeEndedEvent());

        if (comp.CurrentState != CyberpsychosisState.None)
            RollIncrement(comp);
    }

    private void TryRollParalyze(EntityUid uid, CyberpsychosisComponent comp)
    {
        var chance = comp.CurrentState switch
        {
            CyberpsychosisState.Mild => comp.MildParalyzeChance,
            CyberpsychosisState.Moderate => comp.ModerateParalyzeChance,
            CyberpsychosisState.Severe => comp.SevereParalyzeChance,
            _ => 0f
        };

        if (chance > 0f && _random.Prob(chance))
            _stun.TryAddParalyzeDuration(uid, comp.ParalyzeDuration);
    }

    private void RollIncrement(CyberpsychosisComponent comp)
    {
        var (minBase, maxBase) = comp.CurrentState switch
        {
            CyberpsychosisState.Mild => (comp.MildIncrMin, comp.MildIncrMax),
            CyberpsychosisState.Moderate => (comp.ModerateIncrMin, comp.ModerateIncrMax),
            CyberpsychosisState.Severe => (comp.SevereIncrMin, comp.SevereIncrMax),
            _ => (0f, 0f)
        };

        var overflow = comp.CurrentLoad - comp.MaxLoad;
        var scale = 1f + (float)overflow / comp.MaxLoad;
        comp.StressIncrement = _random.NextFloat(minBase * scale, maxBase * scale);
    }

    //TODO: Доделать надо эффекты киберпсихоза
    private void ApplyMildEffects(EntityUid uid, TimeSpan duration, CyberpsychosisComponent comp)
    {
        _hallucinations.StartHallucinations(uid, HallucinationsKey, duration, true, comp.MildHallucinationPack);
        EnsureComp<PainNumbnessStatusEffectComponent>(uid);
    }

    private void ApplyModerateEffects(EntityUid uid, TimeSpan duration, CyberpsychosisComponent comp)
    {
        ApplyMildEffects(uid, duration, comp);
    }

    private void ApplySevereEffects(EntityUid uid, TimeSpan duration, CyberpsychosisComponent comp)
    {
        ApplyModerateEffects(uid, duration, comp);
        EnsureComp<ForceAttackComponent>(uid);
        _faction.AddFaction(uid, RageFaction);
    }

    private void RemoveAllEffects(EntityUid uid)
    {
        _status.TryRemoveStatusEffect(uid, HallucinationsKey);
        RemComp<PainNumbnessStatusEffectComponent>(uid);
        RemCompDeferred<ForceAttackComponent>(uid);
        _faction.RemoveFaction(uid, RageFaction);
    }
}
