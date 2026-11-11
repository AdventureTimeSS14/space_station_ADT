using System.Linq;
using Content.Server.GameTicking.Rules.Components;
using Content.Server.StationEvents.Events;
using Content.Shared.ADT.Virology;
using Content.Shared.ADT.Virology.Prototypes;
using Content.Shared.GameTicking.Components;
using Content.Shared.Humanoid;
using Content.Shared.Mind.Components;
using Content.Shared.Mobs.Systems;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server.ADT.Virology;

public sealed class ADTDiseaseOutbreakRule : StationEventSystem<ADTDiseaseOutbreakRuleComponent>
{
    [Dependency] private ADTDiseaseSystem _disease = default!;
    [Dependency] private MobStateSystem _mobState = default!;
    [Dependency] private IPrototypeManager _proto = default!;

    protected override void Started(EntityUid uid, ADTDiseaseOutbreakRuleComponent component, GameRuleComponent gameRule, GameRuleStartedEvent args)
    {
        base.Started(uid, component, gameRule, args);

        var disease = CreateDisease(component);
        if (disease == null)
            return;

        var candidates = new List<EntityUid>();
        var query = EntityQueryEnumerator<MindContainerComponent, HumanoidProfileComponent>();
        while (query.MoveNext(out var mob, out var mind, out _))
        {
            if (!mind.HasMind || _mobState.IsDead(mob))
                continue;

            candidates.Add(mob);
        }

        RobustRandom.Shuffle(candidates);

        foreach (var candidate in candidates)
        {
            if (_disease.Contract(candidate, disease, isCarrier: true) != null)
                return;
        }
    }

    private ADTDisease? CreateDisease(ADTDiseaseOutbreakRuleComponent component)
    {
        if (component.Diseases.Count == 0 || RobustRandom.Prob(component.AdvanceChance))
        {
            return _disease.CreateRandomVirus(
                resistance: RobustRandom.Next(0, 12),
                stealth: RobustRandom.Pick(new[] { 0, 0, 1, 1, 2 }),
                stageSpeed: RobustRandom.Next(-11, 6),
                transmittable: RobustRandom.Next(5, 10),
                severity: RobustRandom.Next(0, 6));
        }

        var id = RobustRandom.Pick(component.Diseases);
        if (!_proto.TryIndex(id, out var proto))
            return null;

        return proto.Symptoms.Count > 0
            ? _disease.CreatePresetAdvance(proto)
            : ADTDisease.FromPrototype(proto);
    }
}
