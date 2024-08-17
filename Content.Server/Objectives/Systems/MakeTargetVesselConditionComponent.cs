using Content.Server.Objectives.Components;
using Content.Server.Shuttles.Systems;
using Content.Shared.CCVar;
using Content.Shared.Mind;
using Content.Shared.Objectives.Components;
using Content.Shared.Roles.Jobs;
using Robust.Shared.Configuration;
using Robust.Shared.Random;
using Content.Shared.ADT.Phantom.Components;

namespace Content.Server.Objectives.Systems;    // ADT filt

/// <summary>
/// Handles turning person into a vessel
/// </summary>
public sealed class MakeTargetVesselConditionSystem : EntitySystem
{
    [Dependency] private readonly EmergencyShuttleSystem _emergencyShuttle = default!;
    [Dependency] private readonly IConfigurationManager _config = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly SharedJobSystem _job = default!;
    [Dependency] private readonly SharedMindSystem _mind = default!;
    [Dependency] private readonly TargetObjectiveSystem _target = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MakeTargetVesselConditionComponent, ObjectiveGetProgressEvent>(OnGetProgress);

    }

    private void OnGetProgress(EntityUid uid, MakeTargetVesselConditionComponent comp, ref ObjectiveGetProgressEvent args)
    {
        if (!_target.GetTarget(uid, out var target))
            return;

        args.Progress = GetProgress(target.Value, true);
    }

    private float GetProgress(EntityUid target, bool requireDead)
    {
        if (HasComp<VesselComponent>(target))
            return 1f;

        return 0f;
    }
}
