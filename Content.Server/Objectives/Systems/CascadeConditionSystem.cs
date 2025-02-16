using Content.Server.AlertLevel;
using Content.Server.Objectives.Components;
using Content.Shared.Objectives.Components;
using Content.Shared.Mind;
using Content.Server.Station.Systems;

namespace Content.Server.Objectives.Systems;

public sealed class CascadeConditionSystem : EntitySystem
{
    [Dependency] private readonly AlertLevelSystem _alertLevelSystem = default!;
    [Dependency] private readonly StationSystem _stationSystem = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<CascadeConditionComponent, ObjectiveGetProgressEvent>(OnGetProgress);
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

        if (station.HasValue)
        {
            var currentAlertLevel = _alertLevelSystem.GetLevel(station.Value);
            return currentAlertLevel.Equals("cascade", StringComparison.OrdinalIgnoreCase) ? 1f : 0f;
        }

        return 0f;
    }
}