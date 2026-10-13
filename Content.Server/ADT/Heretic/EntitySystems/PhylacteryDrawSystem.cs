using Content.Shared.Body.Components;
using Content.Shared.Body.Systems;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Components.SolutionManager;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.FixedPoint;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Content.Shared.Tag;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.GameObjects;
using Robust.Shared.Prototypes;

namespace Content.Server.ADT.Heretic.EntitySystems;

[RegisterComponent]
public sealed partial class HereticPhylacteryComponent : Component
{
    [DataField]
    public string SolutionName = "phylactery";

    [DataField]
    public SoundSpecifier DrawSound = new SoundPathSpecifier("/Audio/Effects/Chemistry/bubbles.ogg");
}

public sealed class PhylacteryDrawSystem : EntitySystem
{
    [Dependency] private readonly SharedBloodstreamSystem _bloodstream = default!;
    [Dependency] private readonly SharedSolutionContainerSystem _solution = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;

    private const float BloodDrawPercent = 0.15f;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<BloodstreamComponent, InteractUsingEvent>(OnBloodstreamInteractUsing);
        SubscribeLocalEvent<HereticPhylacteryComponent, AfterInteractEvent>(OnAfterInteract);
    }

    private void OnBloodstreamInteractUsing(Entity<BloodstreamComponent> ent, ref InteractUsingEvent args)
    {
        if (args.Handled)
            return;

        if (!TryComp<HereticPhylacteryComponent>(args.Used, out var phylactery))
            return;

        if (TryDrawBlood(args.Used, phylactery, ent.Owner, ent.Comp, args.User))
            args.Handled = true;
    }

    private void OnAfterInteract(Entity<HereticPhylacteryComponent> ent, ref AfterInteractEvent args)
    {
        if (args.Handled || args.Target == null)
            return;

        TryComp<BloodstreamComponent>(args.Target.Value, out var bloodstream);
        if (TryDrawBlood(ent.Owner, ent.Comp, args.Target.Value, bloodstream, args.User))
            args.Handled = true;
    }

    private bool TryDrawBlood(EntityUid phylacteryUid, HereticPhylacteryComponent phylactery, EntityUid target, BloodstreamComponent? bloodstream, EntityUid user)
    {
        if (!_solution.TryGetSolution(phylacteryUid, phylactery.SolutionName, out var phylSol, out var phylSolution))
            return false;

        var availableSpace = phylSolution.AvailableVolume;
        if (availableSpace <= FixedPoint2.Zero)
        {
            _popup.PopupClient(Loc.GetString("phylactery-full"), user, user);
            return true;
        }

        Solution? splitBlood = null;

        if (bloodstream != null && _solution.ResolveSolution(target, bloodstream.BloodSolutionName, ref bloodstream.BloodSolution, out var bloodSolution))
        {
            if (bloodSolution.Volume <= FixedPoint2.Zero)
            {
                _popup.PopupClient(Loc.GetString("phylactery-no-blood"), user, user);
                return true;
            }

            var drainAmount = FixedPoint2.Min(FixedPoint2.New(bloodSolution.Volume.Float() * BloodDrawPercent), availableSpace);
            if (drainAmount < FixedPoint2.New(5))
                drainAmount = FixedPoint2.Min(FixedPoint2.New(15), bloodSolution.Volume);
            drainAmount = FixedPoint2.Min(drainAmount, availableSpace);

            if (drainAmount > FixedPoint2.Zero)
            {
                splitBlood = _solution.SplitSolution(bloodstream.BloodSolution.Value, drainAmount);
            }
        }
        else if (_solution.TryGetDrainableSolution(target, out var drainSol, out var drainSolution))
        {
            if (drainSolution.Volume <= FixedPoint2.Zero)
                return false;

            var drainAmount = FixedPoint2.Min(FixedPoint2.New(15), availableSpace);
            drainAmount = FixedPoint2.Min(drainAmount, drainSolution.Volume);
            if (drainAmount > FixedPoint2.Zero)
            {
                splitBlood = _solution.SplitSolution(drainSol.Value, drainAmount);
            }
        }
        else if (_solution.TryGetDrawableSolution(target, out var drawSol, out var drawSolution))
        {
            if (drawSolution.Volume <= FixedPoint2.Zero)
                return false;

            var drainAmount = FixedPoint2.Min(FixedPoint2.New(15), availableSpace);
            drainAmount = FixedPoint2.Min(drainAmount, drawSolution.Volume);
            if (drainAmount > FixedPoint2.Zero)
            {
                splitBlood = _solution.SplitSolution(drawSol.Value, drainAmount);
            }
        }

        if (splitBlood == null || splitBlood.Volume <= FixedPoint2.Zero)
        {
            _popup.PopupClient(Loc.GetString("phylactery-no-blood"), user, user);
            return true;
        }

        _solution.TryAddSolution(phylSol.Value, splitBlood);
        _audio.PlayPvs(phylactery.DrawSound, phylacteryUid);
        _popup.PopupClient(Loc.GetString("phylactery-draw", ("amount", splitBlood.Volume)), user, user);
        return true;
    }
}
