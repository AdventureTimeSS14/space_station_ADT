using Content.Shared.Actions;
using Content.Shared.PowerCell;
using Content.Shared.PowerCell.Components;
using Content.Server.Power.EntitySystems;
using Content.Server.PowerCell;
using Content.Shared.Alert;
using Content.Shared.Inventory;
using Content.Shared.Movement.Systems;
using Content.Shared.Rounding;

namespace Content.Server.ADT.ShowEnergy;

public sealed class ShowEnergyAlarmSystem : EntitySystem
{
    [Dependency] private readonly AlertsSystem _alertsSystem = default!;
    [Dependency] private readonly PowerCellSystem _powerCellSystem = default!;
    public override void Initialize()
    {
        SubscribeLocalEvent<ShowEnergyAlarmComponent, PowerCellChangedEvent>(OnPowerCellUpdate);
        SubscribeLocalEvent<ShowEnergyAlarmComponent, GetItemActionsEvent>(OnGetActions);
    }
    private void OnPowerCellUpdate(EntityUid uid, ShowEnergyAlarmComponent component, PowerCellChangedEvent args)
    {
        UpdateClothingPowerAlert((uid, component));
    }
    private void OnGetActions(EntityUid uid, ShowEnergyAlarmComponent component, GetItemActionsEvent args)
    {
        component.User = args.User;
        UpdateClothingPowerAlert((uid, component));
    }
    private void UpdateClothingPowerAlert(Entity<ShowEnergyAlarmComponent> entity)
    {
        var (uid, comp) = entity;

        if (!TryComp<PowerCellDrawComponent>(uid, out var drawComp) || entity.Comp.User == null)
            return;

        if (!_powerCellSystem.TryGetBatteryFromSlot(entity, out var battery) || !drawComp.Enabled)
        {
            _alertsSystem.ClearAlert(entity.Comp.User.Value, comp.PowerAlert);
            return;
        }

        var severity = ContentHelpers.RoundToLevels(MathF.Max(0f, battery.CurrentCharge), battery.MaxCharge, 6);
        _alertsSystem.ShowAlert(entity.Comp.User.Value, comp.PowerAlert, (short) severity);
    }
}
