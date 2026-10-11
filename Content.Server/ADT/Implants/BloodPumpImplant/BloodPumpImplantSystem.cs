using Content.Server.Body.Systems;
using Content.Shared.ADT.Implants.BloodPumpImplant;
using Content.Shared.ADT.Implants.ImplantActivationVision;
using Content.Shared.Chemistry.Components;
using Content.Shared.Implants;
using Content.Shared.Implants.Components;
using Content.Shared.Popups;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Timing;

namespace Content.Server.ADT.Implants.BloodPumpImplant;

public sealed class BloodPumpImplantSystem : EntitySystem
{
    [Dependency] private readonly BloodstreamSystem _bloodstream = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedSubdermalImplantSystem _implants = default!;
    [Dependency] private readonly MetaDataSystem _metaData = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    private const float VisionDuration = 1f;

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

        var vision = EnsureComp<ImplantActivationVisionComponent>(user);
        vision.StartTime = _timing.CurTime;
        vision.EndTime = _timing.CurTime + TimeSpan.FromSeconds(VisionDuration);

        UpdateActionDescription(ent);

        if (ent.Comp.RemainingCharges <= 0)
            _implants.ForceRemove(user, ent.Owner);
    }

    private void UpdateActionDescription(Entity<BloodPumpImplantComponent> ent)
    {
        if (!TryComp<SubdermalImplantComponent>(ent.Owner, out var subComp) || subComp.Action is not { } action)
            return;

        _metaData.SetEntityDescription(action, Loc.GetString("blood-pump-implant-action-desc", ("charges", ent.Comp.RemainingCharges)));
    }
}
