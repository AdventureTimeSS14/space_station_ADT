using Content.Shared.ADT.Virology;
using Content.Shared.ADT.Virology.Components;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Chemistry.Reagent;
using Robust.Shared.Prototypes;

namespace Content.Server.ADT.Virology;

public sealed class ADTDiseaseCultureSystem : EntitySystem
{
    [Dependency] private IPrototypeManager _proto = default!;
    [Dependency] private ADTDiseaseSystem _disease = default!;
    [Dependency] private SharedSolutionContainerSystem _solution = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ADTDiseaseCultureComponent, MapInitEvent>(OnMapInit);
    }

    private void OnMapInit(Entity<ADTDiseaseCultureComponent> ent, ref MapInitEvent args)
    {
        if (!_proto.TryIndex(ent.Comp.Disease, out var proto))
            return;

        if (!_solution.TryGetSolution(ent.Owner, ent.Comp.Solution, out var soln, out _))
            return;

        var disease = proto.Symptoms.Count > 0
            ? _disease.CreatePresetAdvance(proto)
            : ADTDisease.FromPrototype(proto);

        var data = new ADTVirusData();
        data.Diseases.Add(disease);

        _solution.TryAddReagent(soln.Value, new ReagentId("Blood", new List<ReagentData> { data }), ent.Comp.Amount, out _);
    }
}
