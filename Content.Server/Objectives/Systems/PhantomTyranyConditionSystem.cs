using Content.Server.Objectives.Components;
using Content.Server.Shuttles.Systems;
using Content.Shared.CCVar;
using Content.Shared.Mind;
using Content.Shared.Objectives.Components;
using Content.Shared.Roles.Jobs;
using Robust.Shared.Configuration;
using Robust.Shared.Random;
using Content.Shared.ADT.Phantom.Components;
using Content.Shared.Mind.Components;
using Content.Server.Station.Systems;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Server.Revolutionary.Components;

namespace Content.Server.Objectives.Systems;    // ADT file

/// <summary>
/// Final phantom objective
/// </summary>
public sealed class PhantomTyranyConditionSystem : EntitySystem
{
    [Dependency] private readonly EmergencyShuttleSystem _emergencyShuttle = default!;
    [Dependency] private readonly IConfigurationManager _config = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly SharedJobSystem _job = default!;
    [Dependency] private readonly SharedMindSystem _mind = default!;
    [Dependency] private readonly TargetObjectiveSystem _target = default!;
    [Dependency] private readonly StationSystem _stationSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PhantomTyranyConditionComponent, ObjectiveGetProgressEvent>(OnGetProgress);

    }

    private void OnGetProgress(EntityUid uid, PhantomTyranyConditionComponent comp, ref ObjectiveGetProgressEvent args)
    {
        args.Progress = GetProgress(args.Mind.OwnedEntity);
    }

    private float GetProgress(EntityUid? uid)
    {
        if (uid == null)
            return 0f;
        if (!TryComp<PhantomComponent>(uid, out var component))
            return 0f;

        var commandList = new List<EntityUid>();

        var heads = AllEntityQuery<CommandStaffComponent>();
        while (heads.MoveNext(out var id, out _))
        {
            commandList.Add(id);
        }

        var dead = 0;
        foreach (var entity in commandList)
        {
            if (TryComp<MobStateComponent>(entity, out var state))
            {
                if (state.CurrentState == MobState.Dead || state.CurrentState == MobState.Invalid)
                {
                    dead++;
                }
                else if (_stationSystem.GetOwningStation(entity) == null && !_emergencyShuttle.EmergencyShuttleArrived)
                {
                    dead++;
                }
            }
            else
            {
                dead++;
            }
        }

        return Math.Clamp(dead / commandList.Count, 0f, component.TyranyStarted ? 1f : 0.8f);
    }
}
