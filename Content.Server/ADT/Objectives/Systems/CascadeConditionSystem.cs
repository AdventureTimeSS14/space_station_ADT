using Content.Shared.AlertLevel;
using Content.Server.Objectives.Components;
using Content.Shared.Objectives.Components;
using Content.Server.ADT.Objectives.Components;
using Content.Shared.Mind;
using Content.Server.Station.Systems;

namespace Content.Server.ADT.Objectives.Systems;

public sealed class CascadeConditionSystem : EntitySystem
{
    [Dependency] private AlertLevelSystem _alertLevelSystem = default!;
    [Dependency] private StationSystem _stationSystem = default!;
    [Dependency] private CheckSupermatterSystem _supermatter = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CascadeConditionComponent, ObjectiveAssignedEvent>(OnAssigned);
        SubscribeLocalEvent<CascadeConditionComponent, ObjectiveGetProgressEvent>(OnGetProgress);
    }

    private void OnAssigned(Entity<CascadeConditionComponent> condition, ref ObjectiveAssignedEvent args)
    {
        if (condition.Comp.Supermatter && !_supermatter.SupermatterCheck())
        {
            args.Cancelled = true;
            return;
        }
    }

    private void OnGetProgress(EntityUid uid, CascadeConditionComponent comp, ref ObjectiveGetProgressEvent args)
    {
        args.Progress = GetProgress(args.MindId, args.Mind);
    }

    private float GetProgress(EntityUid mindId, MindComponent mind)
    {
        if (!mind.OwnedEntity.HasValue)
            return 0f;

        var stationUid = mind.OwnedEntity.Value;
        var station = _stationSystem.GetOwningStation(stationUid);
        // Checking for the existence of a station
        if (station.HasValue)
        {
            // Taking information about AlertLevel on station.
            // If station under cascade - mission complited.
            return _alertLevelSystem.TryGetLevel(station.Value, out var currentAlertLevel) && currentAlertLevel.Value.Id == "Cascade" ? 1f : 0f;
        }

        return 0f;
    }
}