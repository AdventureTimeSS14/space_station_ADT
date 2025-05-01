using Content.Shared.ADT.Silicons.Borgs;
using Robust.Server.GameObjects;
using Content.Server.Actions;
using Robust.Shared.Player;
using Content.Server.AlertLevel;
using Content.Server.Station.Systems;
using Content.Shared.PowerCell.Components;
using Content.Server.PowerCell;
using Content.Shared.Item.ItemToggle;
using Content.Shared.PowerCell;

namespace Content.Server.ADT.Silicons.Borgs;

public sealed partial class BorgInfoSystem : EntitySystem
{
    [Dependency] private readonly UserInterfaceSystem _uiSystem = default!;
    [Dependency] private readonly ActionsSystem _action = default!;
    [Dependency] private readonly StationSystem _station = default!;
    [Dependency] private readonly PowerCellSystem _powerCell = default!;
    [Dependency] private readonly ItemToggleSystem _toggle = default!;
    [Dependency] private readonly IEntityManager _entity = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<BorgInfoComponent, BoundUIOpenedEvent>(OnWindowOpen);
        SubscribeLocalEvent<BorgInfoComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<BorgInfoComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<BorgInfoComponent, BorgInfoActionEvent>(ScreenInfoAction);
        SubscribeLocalEvent<StationRenamedEvent>(OnStationRenamed);
        SubscribeLocalEvent<AlertLevelChangedEvent>(OnAlertLevelChanged);

        SubscribeLocalEvent<BorgInfoComponent, PowerCellChangedEvent>(OnPowerCellChanged);
        SubscribeLocalEvent<BorgInfoComponent, PowerCellSlotEmptyEvent>(OnPowerCellSlotEmpty);
    }

    private void OnWindowOpen(Entity<BorgInfoComponent> ent, ref BoundUIOpenedEvent args)
    {
        if (!BorgInfoUiKey.BorgInfo.Equals(args.UiKey))
            return;

        UpdateWindow(ent.Owner, ent.Comp);
    }

    private void OnMapInit(EntityUid uid, BorgInfoComponent component, MapInitEvent args)
    {
        _action.AddAction(uid, ref component.ActionEntity, component.Action);
    }
    private void OnShutdown(EntityUid uid, BorgInfoComponent component, ComponentShutdown args)
    {
        _action.RemoveAction(uid, component.ActionEntity);
    }
    private void ScreenInfoAction(EntityUid uid, BorgInfoComponent comp, BorgInfoActionEvent args)
    {
        if (args.Handled)
            return;

        if (!TryComp<ActorComponent>(uid, out var actor))
            return;

        _uiSystem.TryOpenUi(uid, BorgInfoUiKey.BorgInfo, actor.Owner);

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
        var query = AllEntityQuery<BorgInfoComponent>();
        while (query.MoveNext(out var ent, out var comp))
        {
            UpdateWindow(ent, comp);
        }
    }
    public void UpdateWindow(EntityUid uid, BorgInfoComponent? component = null)
    {
        if (!Resolve(uid, ref component, false))
            return;

        if (!_uiSystem.HasUi(uid, BorgInfoUiKey.BorgInfo))
            return;

        var chargePercent = 0f;
        var hasBattery = false;

        if (_powerCell.TryGetBatteryFromSlot(uid, out var battery))
        {
            chargePercent = battery.CurrentCharge / battery.MaxCharge;
            hasBattery = true;
        }
        var borgName = _entity.GetComponent<MetaDataComponent>(uid).EntityName;


        var state = new BorgInfoUpdateState(
            borgName,
            new BorgInfoStation
            {
                StationName = component.StationName,
                StationAlertLevel = component.StationAlertLevel,
                StationAlertColor = component.StationAlertColor
            },
            chargePercent,
            hasBattery
                );

        _uiSystem.SetUiState(uid, BorgInfoUiKey.BorgInfo, state);
        UpdateStationName(uid, component);
        UpdateAlertLevel(uid, component);
    }
    private void OnPowerCellChanged(EntityUid uid, BorgInfoComponent component, PowerCellChangedEvent args)
    {

        if (_powerCell.HasDrawCharge(uid))
        {
            _toggle.TryActivate(uid);
        }

        UpdateWindow(uid, component);
    }
    private void OnPowerCellSlotEmpty(EntityUid uid, BorgInfoComponent component, ref PowerCellSlotEmptyEvent args)
    {
        _toggle.TryDeactivate(uid);
        UpdateWindow(uid, component);
    }

    private void UpdateStationName(EntityUid uid, BorgInfoComponent component)
    {
        var station = _station.GetOwningStation(uid);
        component.StationName = station is null ? null : Name(station.Value);
    }

    private void UpdateAlertLevel(EntityUid uid, BorgInfoComponent component)
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
