using Content.Server.Body.Systems;
using Content.Shared.ADT.BloodPumpImplant;
using Content.Shared.Chemistry.Components;
using Content.Shared.Popups;
using Robust.Shared.Audio.Systems;

namespace Content.Server.ADT.BloodPumpImplant;

public sealed class BloodPumpImplantSystem : EntitySystem
{
    [Dependency] private readonly BloodstreamSystem _bloodstream = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<BloodPumpImplantComponent, ActivateBloodPumpImplantActionEvent>(OnActivate);
    }

    private void OnActivate(Entity<BloodPumpImplantComponent> ent, ref ActivateBloodPumpImplantActionEvent args)
    {
        if (args.Handled)
            return;

        var user = args.Performer;

        if (ent.Comp.RemainingCharges <= 0)
        {
            _popup.PopupEntity(Loc.GetString("blood-pump-implant-depleted"), user, user, PopupType.SmallCaution);
            return;
        }

        var solution = new Solution();
        foreach (var (reagent, amount) in ent.Comp.Reagents)
            solution.AddReagent(reagent, amount);

        if (!_bloodstream.TryAddToBloodstream(user, solution))
        {
            _popup.PopupEntity(Loc.GetString("blood-pump-implant-full"), user, user, PopupType.SmallCaution);
            return;
        }

        ent.Comp.RemainingCharges--;
        args.Handled = true;
        _audio.PlayPvs(ent.Comp.Sound, user);
        _popup.PopupEntity(Loc.GetString("blood-pump-implant-activate", ("charges", ent.Comp.RemainingCharges)), user, user, PopupType.Small);
    }
}
