using Content.Server.Fluids.EntitySystems;
using Content.Shared.ADT.Heretic.Components.Side;
using Content.Shared.Body.Components;
using Content.Shared.Body.Systems;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Components.SolutionManager;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.FixedPoint;
using Content.Shared.Fluids.Components;
using Content.Shared.Interaction;
using Content.Shared.Mobs.Components;
using Content.Shared.Popups;
using Content.Shared.Tag;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.GameObjects;
using Robust.Shared.Prototypes;

namespace Content.Server.ADT.Heretic.EntitySystems;

public sealed class PhylacteryDrawSystem : EntitySystem
{
    [Dependency] private readonly SharedBloodstreamSystem _bloodstream = default!;
    [Dependency] private readonly SharedSolutionContainerSystem _solution = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly PuddleSystem _puddle = default!;

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

    public void OnMobInteractUsing(Entity<MobStateComponent> ent, ref InteractUsingEvent args)
    {
        if (args.Handled)
            return;

        if (!TryComp<HereticPhylacteryComponent>(args.Used, out var phylactery))
            return;

        TryComp<BloodstreamComponent>(ent.Owner, out var bloodstream);
        if (TryDrawBlood(args.Used, phylactery, ent.Owner, bloodstream, args.User))
            args.Handled = true;
    }

    private void OnAfterInteract(Entity<HereticPhylacteryComponent> ent, ref AfterInteractEvent args)
    {
        if (args.Handled)
            return;

        EntityUid? target = args.Target;

        if (target != null && TryComp<HereticPhylacteryComponent>(ent.Owner, out var phylactery))
        {
            TryComp<BloodstreamComponent>(target.Value, out var bloodstream);
            if (TryDrawBlood(ent.Owner, phylactery, target.Value, bloodstream, args.User))
            {
                args.Handled = true;
                return;
            }
        }

        if (target == null)
        {
            foreach (var near in _lookup.GetEntitiesInRange(args.ClickLocation, 0.8f))
            {
                if (HasComp<BloodstreamComponent>(near) || HasComp<MobStateComponent>(near) || HasComp<PuddleComponent>(near))
                {
                    target = near;
                    break;
                }
            }
        }

        if (target != null && TryComp<HereticPhylacteryComponent>(ent.Owner, out var phyl))
        {
            TryComp<BloodstreamComponent>(target.Value, out var bs);
            if (TryDrawBlood(ent.Owner, phyl, target.Value, bs, args.User))
            {
                args.Handled = true;
                return;
            }
        }

        if (!_solution.TryGetSolution(ent.Owner, ent.Comp.SolutionName, out var phylSol, out var phylSolution))
            return;

        if (phylSolution.Volume <= FixedPoint2.Zero)
        {
            _popup.PopupClient(Loc.GetString("phylactery-empty"), args.User, args.User);
            return;
        }

        var spillAmount = FixedPoint2.Min(FixedPoint2.New(10), phylSolution.Volume);
        var splitBlood = _solution.SplitSolution(phylSol.Value, spillAmount);
        if (_puddle.TrySpillAt(args.ClickLocation, splitBlood, out _, sound: true))
        {
            _audio.PlayPvs(new SoundPathSpecifier("/Audio/Effects/Fluids/splat.ogg"), ent.Owner);
            _popup.PopupClient(Loc.GetString("phylactery-spill", ("amount", splitBlood.Volume)), args.User, args.User);
            args.Handled = true;
        }
        else
        {
            _solution.TryAddSolution(phylSol.Value, splitBlood);
        }
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

        if (bloodstream == null)
            TryComp(target, out bloodstream);

        if (bloodstream != null && _solution.ResolveSolution(target, bloodstream.BloodSolutionName, ref bloodstream.BloodSolution, out var bloodSolution))
        {
            if (bloodSolution.Volume <= FixedPoint2.Zero)
            {
                _popup.PopupClient(Loc.GetString("phylactery-no-blood"), user, user);
                return true;
            }

            var drainAmount = FixedPoint2.Min(FixedPoint2.New(15), availableSpace);
            drainAmount = FixedPoint2.Min(drainAmount, bloodSolution.Volume);

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
        else if (TryComp<PuddleComponent>(target, out var puddle) && _solution.TryGetSolution(target, puddle.SolutionName, out var puddleSol, out var puddleSolution))
        {
            if (puddleSolution.Volume <= FixedPoint2.Zero)
                return false;

            var drainAmount = FixedPoint2.Min(FixedPoint2.New(15), availableSpace);
            drainAmount = FixedPoint2.Min(drainAmount, puddleSolution.Volume);
            if (drainAmount > FixedPoint2.Zero)
            {
                splitBlood = _solution.SplitSolution(puddleSol.Value, drainAmount);
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
