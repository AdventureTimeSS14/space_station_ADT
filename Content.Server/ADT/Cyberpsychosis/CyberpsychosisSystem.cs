using Content.Server.ADT.Hallucinations;
using Content.Shared.ADT.Cyberpsychosis;
using Content.Shared.Implants;
using Content.Shared.Implants.Components;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server.ADT.Cyberpsychosis;

public sealed class CyberpsychosisSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly HallucinationsSystem _hallucinations = default!;

    private const string HallucinationsKey = "ADTHallucinations";
    private static readonly TimeSpan StressTickInterval = TimeSpan.FromSeconds(1);

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<SubdermalImplantComponent, ImplantImplantedEvent>(OnImplantAdded);
        SubscribeLocalEvent<SubdermalImplantComponent, ImplantRemovedEvent>(OnImplantRemoved);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);
        var query = EntityQueryEnumerator<CyberpsychosisComponent>();
        while (query.MoveNext(out var uid, out var comp))
            UpdatePsychosis(uid, comp);
    }

    private void OnImplantAdded(EntityUid uid, SubdermalImplantComponent implantComp, ref ImplantImplantedEvent args)
    {
        if (!TryComp<CyberneticLoadComponent>(uid, out var cyberImplant))
            return;

        var psychosis = EnsureComp<CyberpsychosisComponent>(args.Implanted);
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

        EnsureComp<ActiveCyberpsychosisComponent>(uid);
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
        RaiseLocalEvent(uid, new CyberpsychosisEpisodeEndedEvent());

        if (comp.CurrentState != CyberpsychosisState.None)
            RollIncrement(comp);
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
    }

    private void ApplyModerateEffects(EntityUid uid, TimeSpan duration, CyberpsychosisComponent comp) { }
    private void ApplySevereEffects(EntityUid uid, TimeSpan duration, CyberpsychosisComponent comp) { }
    private void RemoveAllEffects(EntityUid uid) { }
}
