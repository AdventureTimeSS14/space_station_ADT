using Content.Server.Body.Systems;
using Content.Shared.ADT.Implants.ImplantActivationVision;
using Content.Shared.ADT.Implants.StimulantImplant;
using Content.Shared.Chemistry.Components;
using Content.Shared.Implants;
using Content.Shared.Implants.Components;
using Content.Shared.Popups;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Timing;

namespace Content.Server.ADT.Implants.StimulantImplant;

public sealed class StimulantImplantSystem : EntitySystem
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

        var solution = new Solution();
        solution.AddReagent(ent.Comp.Reagent, ent.Comp.Amount);

        if (!_bloodstream.TryAddToBloodstream(user, solution))
        {
            _popup.PopupEntity(Loc.GetString("stimulant-implant-full"), user, user, PopupType.SmallCaution);
            return;
        }

        ent.Comp.RemainingCharges--;
        args.Handled = true;
        _audio.PlayPvs(ent.Comp.Sound, user);
        _popup.PopupEntity(Loc.GetString("stimulant-implant-activate", ("charges", ent.Comp.RemainingCharges)), user, user, PopupType.Small);

        var vision = EnsureComp<ImplantActivationVisionComponent>(user);
        vision.StartTime = _timing.CurTime;
        vision.EndTime = _timing.CurTime + TimeSpan.FromSeconds(VisionDuration);

        UpdateActionDescription(ent);

        if (ent.Comp.RemainingCharges <= 0)
            _implants.ForceRemove(user, ent.Owner);
    }

    private void UpdateActionDescription(Entity<StimulantImplantComponent> ent)
    {
        if (!TryComp<SubdermalImplantComponent>(ent.Owner, out var subComp) || subComp.Action is not { } action)
            return;

        _metaData.SetEntityDescription(action, Loc.GetString("stimulant-implant-action-desc", ("charges", ent.Comp.RemainingCharges)));
    }
}
