using Content.Shared.ADT.Ghost;
using Robust.Server.GameObjects;
using Content.Server.Actions;
using Robust.Shared.Player;
using Content.Server.AlertLevel;
using Content.Server.Station.Systems;

namespace Content.Server.ADT.Ghost;

public sealed partial class GhostInfoSystem : EntitySystem
{
    [Dependency] private readonly UserInterfaceSystem _uiSystem = default!;
    [Dependency] private readonly ActionsSystem _action = default!;
    [Dependency] private readonly StationSystem _station = default!;
    [Dependency] private readonly IEntityManager _entity = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<GhostInfoComponent, BoundUIOpenedEvent>(OnWindowOpen);
        SubscribeLocalEvent<GhostInfoComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<GhostInfoComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<GhostInfoComponent, GhostInfoActionEvent>(GhostInfoAction);

        SubscribeLocalEvent<StationRenamedEvent>(OnStationRenamed);
        SubscribeLocalEvent<AlertLevelChangedEvent>(OnAlertLevelChanged);
    }

    private void OnWindowOpen(Entity<GhostInfoComponent> ent, ref BoundUIOpenedEvent args)
    {
        if (!GhostInfoUiKey.Key.Equals(args.UiKey))
            return;

        UpdateAllPdaUisOnStation();
    }

    private void OnMapInit(EntityUid uid, GhostInfoComponent component, MapInitEvent args)
    {
        _action.AddAction(uid, ref component.ActionEntity, component.Action);
    }

    private void OnShutdown(EntityUid uid, GhostInfoComponent component, ComponentShutdown args)
    {
        _action.RemoveAction(uid, component.ActionEntity);
    }

    private void GhostInfoAction(EntityUid uid, GhostInfoComponent comp, GhostInfoActionEvent args)
    {
        if (args.Handled)
            return;

        if (!TryComp<ActorComponent>(uid, out var actor))
            return;

        _uiSystem.TryOpenUi(uid, GhostInfoUiKey.Key, actor.Owner);

        args.Handled = true;
    }

    private void OnStationRenamed(StationRenamedEvent ev)
    {
        UpdateAllPdaUisOnStation();
    }

    private void OnAlertLevelChanged(AlertLevelChangedEvent args)
    {
        UpdateAllPdaUisOnStation();
    }

    private void UpdateAllPdaUisOnStation()
    {
        var query = AllEntityQuery<GhostInfoComponent>();
        while (query.MoveNext(out var ent, out var comp))
        {
            UpdateWindow(ent, comp);
        }
    }

    public void UpdateWindow(EntityUid uid, GhostInfoComponent? component = null)
    {
        if (!Resolve(uid, ref component, false))
            return;

        if (!_uiSystem.HasUi(uid, GhostInfoUiKey.Key))
            return;

        UpdateAlertLevel(uid, component);

        var state = new GhostInfoUpdateState(
            component.StationAlertLevel,
            component.StationAlertColor
            );

        _uiSystem.SetUiState(uid, GhostInfoUiKey.Key, state);
    }

    private void UpdateAlertLevel(EntityUid uid, GhostInfoComponent component)
    {
        var station = _station.GetOwningStation(uid);
        if (!TryComp(station, out AlertLevelComponent? alertComp) ||
        alertComp.AlertLevels == null)
            return;
        component.StationAlertLevel = alertComp.CurrentLevel;
        if (alertComp.AlertLevels.Levels.TryGetValue(alertComp.CurrentLevel, out var details))
            component.StationAlertColor = details.Color;
    }
}
