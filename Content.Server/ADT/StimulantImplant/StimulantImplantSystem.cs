using Content.Shared.ADT.StimulantImplant;
using Content.Shared.Body.Components;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Popups;
using Robust.Shared.Audio.Systems;

namespace Content.Server.ADT.StimulantImplant;

public sealed class StimulantImplantSystem : EntitySystem
{
    [Dependency] private readonly SharedSolutionContainerSystem _solutionContainer = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<StimulantImplantComponent, ActivateStimulantImplantActionEvent>(OnActivate);
    }

    private void OnActivate(Entity<StimulantImplantComponent> ent, ref ActivateStimulantImplantActionEvent args)
    {
        if (args.Handled)
            return;

        var user = args.Performer;

        if (ent.Comp.RemainingCharges <= 0)
        {
            _popup.PopupEntity(Loc.GetString("stimulant-implant-depleted"), user, user, PopupType.SmallCaution);
            return;
        }

        if (!TryComp<BloodstreamComponent>(user, out var bloodstream))
            return;

        if (!_solutionContainer.ResolveSolution(user, bloodstream.MetabolitesSolutionName, ref bloodstream.MetabolitesSolution, out _))
            return;

        var solution = new Solution();
        solution.AddReagent(ent.Comp.Reagent, ent.Comp.Amount);

        if (!_solutionContainer.TryAddSolution(bloodstream.MetabolitesSolution!.Value, solution))
        {
            _popup.PopupEntity(Loc.GetString("stimulant-implant-full"), user, user, PopupType.SmallCaution);
            return;
        }

        ent.Comp.RemainingCharges--;
        args.Handled = true;
        _audio.PlayPvs(ent.Comp.Sound, user);
        _popup.PopupEntity(Loc.GetString("stimulant-implant-activate", ("charges", ent.Comp.RemainingCharges)), user, user, PopupType.Small);
    }
}
