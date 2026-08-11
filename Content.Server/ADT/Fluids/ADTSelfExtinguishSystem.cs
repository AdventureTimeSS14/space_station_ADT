using Content.Server.Atmos.EntitySystems;
using Content.Server.Fluids.EntitySystems;
using Content.Shared.ADT.Fluids;
using Content.Shared.Atmos.Components;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Fluids;
using Content.Shared.Fluids.Components;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Content.Shared.Timing;
using Robust.Shared.Audio.Systems;

namespace Content.Server.ADT.Fluids;

public sealed class ADTSelfExtinguishSystem : EntitySystem
{
    [Dependency] private readonly FlammableSystem _flammable = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedSolutionContainerSystem _solutionContainer = default!;
    [Dependency] private readonly UseDelaySystem _useDelay = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SelfSprayComponent, AfterInteractEvent>(OnAfterInteract, before: new[] { typeof(SpraySystem) });
    }

    private void OnAfterInteract(Entity<SelfSprayComponent> ent, ref AfterInteractEvent args)
    {
        if (args.Handled || args.Target != args.User)
            return;

        if (!TryComp<SprayComponent>(ent, out var spray))
            return;

        args.Handled = TryExtinguishSelf((ent.Owner, spray), args.User);
    }

    private bool TryExtinguishSelf(Entity<SprayComponent> ent, EntityUid user)
    {
        if (!TryComp<FlammableComponent>(user, out var flammable) || !flammable.OnFire)
            return false;

        var attempt = new SprayAttemptEvent(user);
        RaiseLocalEvent(ent, ref attempt);

        if (attempt.Cancelled)
        {
            if (attempt.CancelPopupMessage != null)
                _popup.PopupEntity(Loc.GetString(attempt.CancelPopupMessage), ent.Owner, user);

            return true;
        }

        if (_useDelay.IsDelayed((ent.Owner, null)))
            return true;

        if (!_solutionContainer.TryGetSolution(ent.Owner, ent.Comp.Solution, out var soln, out var solution)
            || solution.Volume <= 0)
        {
            _popup.PopupEntity(Loc.GetString(ent.Comp.SprayEmptyPopupMessage, ("entity", ent.Owner)), ent.Owner, user);
            return true;
        }

        _solutionContainer.SplitSolution(soln.Value, ent.Comp.TransferAmount);

        _flammable.Extinguish(user, flammable);

        _audio.PlayPvs(ent.Comp.SpraySound, ent.Owner, ent.Comp.SpraySound.Params.WithVariation(0.125f));
        _popup.PopupEntity(Loc.GetString("adt-spray-self-extinguish"), user, user);

        _useDelay.TryResetDelay(ent.Owner);

        return true;
    }
}
