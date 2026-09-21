using Content.Server.Atmos.Components;
using Content.Server.Atmos.EntitySystems;
using Content.Server.Beam;
using Content.Server.Popups;
using Content.Shared.ADT.RPD;

namespace Content.Server.ADT.RPD;

public sealed class RPDBluespaceSystem : EntitySystem
{
    [Dependency] private readonly BeamSystem _beam = default!;
    [Dependency] private readonly PipeRestrictOverlapSystem _pipeRestrictOverlap = default!;
    [Dependency] private readonly PopupSystem _popup = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<RPDInstantPlacementEvent>(OnInstantPlacement);
        SubscribeLocalEvent<RPDPlacementValidatedEvent>(OnPlacementValidated);
    }

    private void OnInstantPlacement(RPDInstantPlacementEvent ev)
    {
        _beam.TryCreateBeam(ev.User, ev.Target, ev.BeamPrototype);
    }

    private void OnPlacementValidated(RPDPlacementValidatedEvent ev)
    {
        if (!HasComp<PipeRestrictOverlapComponent>(ev.Entity) ||
            !_pipeRestrictOverlap.CheckOverlap(ev.Entity))
            return;

        _popup.PopupEntity(Loc.GetString("pipe-restrict-overlap-popup-blocked", ("pipe", ev.Entity)), ev.Entity, ev.User);
        ev.Rejected = true;
    }
}
