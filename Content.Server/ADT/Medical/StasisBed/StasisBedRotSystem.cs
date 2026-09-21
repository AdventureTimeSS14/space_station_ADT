using Content.Server.Atmos.Rotting;
using Content.Server.Power.EntitySystems;
using Content.Shared.ADT.Construction;
using Content.Shared.ADT.Construction.Events;
using Content.Shared.Atmos.Rotting;
using Content.Shared.Body.Components;
using Content.Shared.Buckle.Components;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.EntitySystems;

namespace Content.Server.ADT.Medical.StasisBed;
public sealed class StasisBedRotSystem : EntitySystem
{
    [Dependency] private readonly PowerReceiverSystem _power = default!;
    [Dependency] private readonly SharedSolutionContainerSystem _solution = default!;
    [Dependency] private readonly RottingSystem _rotting = default!;

    private float _accumulator;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<StasisBedRotComponent, RefreshPartsEvent>(OnRefreshParts);
        SubscribeLocalEvent<StasisBedRotComponent, UpgradeExamineEvent>(OnUpgradeExamine);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        _accumulator += frameTime;
        if (_accumulator < 1f)
            return;

        var elapsed = TimeSpan.FromSeconds(_accumulator);
        _accumulator = 0f;

        var query = EntityQueryEnumerator<StasisBedRotComponent, StrapComponent>();
        while (query.MoveNext(out var uid, out var rot, out var strap))
        {
            if (!_power.IsPowered(uid))
                continue;

            foreach (var patient in strap.BuckledEntities)
            {
                if (rot.Tier < rot.RotStopTier)
                    TrySlowRot(patient, rot, elapsed);

                if (rot.Tier >= rot.InaprovalineTier)
                    TryStabilizePatient(patient, rot);
            }
        }
    }

    private void TrySlowRot(EntityUid patient, StasisBedRotComponent rot, TimeSpan elapsed)
    {
        if (rot.SlowMultiplier is <= 0f or >= 1f
            || HasComp<RottingComponent>(patient)
            || !_rotting.IsRotProgressing(patient, null))
        {
            return;
        }

        _rotting.ReduceAccumulator(patient, elapsed * (1f - rot.SlowMultiplier));
    }

    private void OnRefreshParts(EntityUid uid, StasisBedRotComponent component, RefreshPartsEvent args)
    {
        var servoTier = args.GetPartRating(MachinePartIds.Servo, 1f);
        var capacitorTier = args.GetPartRating(MachinePartIds.Capacitor, 1f);

        component.Tier = Math.Min(servoTier, capacitorTier);

        if (component.Tier >= component.RotStopTier)
            EnsureComp<AntiRotOnBuckleComponent>(uid);
        else
            RemComp<AntiRotOnBuckleComponent>(uid);
    }

    private void OnUpgradeExamine(EntityUid uid, StasisBedRotComponent component, UpgradeExamineEvent args)
    {
        if (component.Tier >= component.RotStopTier)
            args.AddUpgradeLine(Loc.GetString("stasis-bed-rot-stopped"));
        else
            args.AddUpgradeLine(Loc.GetString("stasis-bed-rot-not-stopped",
                ("percent", (int) ((1f - component.SlowMultiplier) * 100f))));

        if (component.Tier >= component.InaprovalineTier)
            args.AddUpgradeLine(Loc.GetString("stasis-bed-inaprovaline", ("amount", component.InaprovalineAmount)));
        else
            args.AddUpgradeLine(Loc.GetString("stasis-bed-inaprovaline-unavailable", ("tier", component.InaprovalineTier)));
    }

    private void TryStabilizePatient(EntityUid patient, StasisBedRotComponent rot)
    {
        if (!TryComp<BloodstreamComponent>(patient, out var bloodstream)
            || !_solution.TryGetSolution(patient, bloodstream.BloodSolutionName, out var solEnt, out var solution))
        {
            return;
        }

        var current = solution.GetTotalPrototypeQuantity(rot.InaprovalineReagent);
        if (current >= rot.InaprovalineAmount)
            return;

        solution.AddReagent(rot.InaprovalineReagent, rot.InaprovalineAmount - current);
        _solution.UpdateChemicals(solEnt.Value);
    }
}
