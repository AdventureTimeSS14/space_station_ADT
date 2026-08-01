using Content.Shared.Power.Components;
using Content.Shared.Power.EntitySystems;
using Content.Shared.PowerCell;
using Content.Shared.PowerCell.Components;
using Content.Server.Power.EntitySystems;
using Content.Shared.Alert;
using Content.Shared.Inventory;
using Content.Shared.Inventory.Events;
using Content.Shared.Movement.Systems;
using Content.Shared.Rounding;

namespace Content.Server.ADT.ShowEnergy;

public sealed class ShowEnergyAlarmSystem : EntitySystem
{
    [Dependency] private readonly AlertsSystem _alertsSystem = default!;
    [Dependency] private readonly PowerCellSystem _powerCellSystem = default!;
    [Dependency] private readonly SharedBatterySystem _batterySystem = default!;

    private float _updateAccumulator;

    public override void Initialize()
    {
        SubscribeLocalEvent<ShowEnergyAlarmComponent, PowerCellChangedEvent>(OnPowerCellUpdate);
        SubscribeLocalEvent<ShowEnergyAlarmComponent, GotEquippedEvent>(OnEquipped);
        SubscribeLocalEvent<ShowEnergyAlarmComponent, GotUnequippedEvent>(OnUnequipped);
    }

    public override void Update(float frameTime)
    {
        _updateAccumulator += frameTime;
        if (_updateAccumulator < 1f)
            return;

        _updateAccumulator = 0f;

        var query = EntityQueryEnumerator<ShowEnergyAlarmComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if (comp.User != null)
                UpdateClothingPowerAlert((uid, comp));
        }
    }

    private void OnPowerCellUpdate(EntityUid uid, ShowEnergyAlarmComponent component, PowerCellChangedEvent args)
    {
        UpdateClothingPowerAlert((uid, component));
    }

    private void OnEquipped(EntityUid uid, ShowEnergyAlarmComponent component, GotEquippedEvent args)
    {
        component.User = args.Equipee;
        UpdateClothingPowerAlert((uid, component));
    }

    private void OnUnequipped(EntityUid uid, ShowEnergyAlarmComponent component, GotUnequippedEvent args)
    {
        if (component.User != null)
            _alertsSystem.ClearAlert(component.User.Value, component.PowerAlert);

        component.User = null;
    }

    private void UpdateClothingPowerAlert(Entity<ShowEnergyAlarmComponent> entity)
    {
        var (uid, comp) = entity;

        if (!TryComp<PowerCellDrawComponent>(uid, out var drawComp) || entity.Comp.User == null)
            return;

        if (!TryComp<PowerCellSlotComponent>(uid, out var cellSlot)
            || !_powerCellSystem.TryGetBatteryFromSlot((uid, cellSlot), out var battery)
            || !drawComp.Enabled)
        {
            _alertsSystem.ClearAlert(entity.Comp.User.Value, comp.PowerAlert);
            return;
        }

        if (!TryComp<BatteryComponent>(battery!.Value, out var batteryComp))
            return;

        var severity = ContentHelpers.RoundToLevels(MathF.Max(0f, _batterySystem.GetCharge(battery.Value.AsNullable())), batteryComp.MaxCharge, 6);
        _alertsSystem.ShowAlert(entity.Comp.User.Value, comp.PowerAlert, (short) severity);
    }
}
