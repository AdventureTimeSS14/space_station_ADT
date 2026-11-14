using System.Linq;
using Content.Shared.ADT.Virology.Components;
using Content.Shared.ADT.Virology.Pandemic;
using Content.Shared.Body.Components;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Forensics.Components;
using Content.Shared.Verbs;
using Robust.Server.GameObjects;

namespace Content.Server.ADT.Virology;

public sealed class ADTDiseaseViewerSystem : EntitySystem
{
    [Dependency] private readonly ADTPandemicSystem _pandemic = default!;
    [Dependency] private readonly SharedSolutionContainerSystem _solution = default!;
    [Dependency] private readonly UserInterfaceSystem _ui = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ADTDiseaseViewerComponent, GetVerbsEvent<UtilityVerb>>(OnGetVerbs);
    }

    private void OnGetVerbs(Entity<ADTDiseaseViewerComponent> ent, ref GetVerbsEvent<UtilityVerb> args)
    {
        if (!args.CanAccess || !args.CanInteract)
            return;

        var target = args.Target;
        if (!HasComp<ADTDiseaseCarrierComponent>(target)
            && !HasComp<BloodstreamComponent>(target)
            && !_solution.TryGetDrainableSolution(target, out _, out _))
        {
            return;
        }

        var user = args.User;
        args.Verbs.Add(new UtilityVerb
        {
            Text = Loc.GetString("adt-disease-viewer-verb"),
            Act = () => Scan(ent, user, target),
        });
    }

    private void Scan(Entity<ADTDiseaseViewerComponent> ent, EntityUid user, EntityUid target)
    {
        var state = new ADTPandemicBoundUserInterfaceState
        {
            HasBeaker = true,
            ReadOnly = true,
        };

        if (HasComp<ADTDiseaseCarrierComponent>(target) || HasComp<BloodstreamComponent>(target))
        {
            state.BloodDna = CompOrNull<DnaComponent>(target)?.DNA ?? Loc.GetString("adt-pandemic-unknown");
            if (TryComp<ADTDiseaseCarrierComponent>(target, out var carrier))
                _pandemic.FillDiseases(state, carrier.Diseases, carrier.Resistances, ent.Comp.IgnoreStealth);
        }
        else if (_solution.TryGetDrainableSolution(target, out _, out var solution)
            && ADTDiseaseSystem.FindVirusData(solution, out var reagent) is { } data)
        {
            state.BloodDna = reagent.Reagent.Data?
                .OfType<DnaData>()
                .FirstOrDefault()?.DNA ?? Loc.GetString("adt-pandemic-unknown");
            _pandemic.FillDiseases(state, data.Diseases, data.Resistances, ent.Comp.IgnoreStealth);
        }
        else
        {
            state.ErrorMessage = "adt-pandemic-error-no-blood";
        }

        _ui.OpenUi(ent.Owner, ADTPandemicUiKey.Key, user);
        _ui.SetUiState(ent.Owner, ADTPandemicUiKey.Key, state);
    }
}
