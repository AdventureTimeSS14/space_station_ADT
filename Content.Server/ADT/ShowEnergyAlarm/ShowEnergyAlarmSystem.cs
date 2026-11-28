using Content.Shared.Alert;
using Content.Shared.Clothing.Components;
using Content.Shared.Inventory.Events;
using Content.Shared.Power.EntitySystems;
using Content.Shared.PowerCell;
using Content.Shared.PowerCell.Components;
using Content.Shared.Rounding;

namespace Content.Server.ADT.ShowEnergy;

public sealed class ShowEnergyAlarmSystem : EntitySystem
{
    [Dependency] private AlertsSystem _alerts = default!;
    [Dependency] private PowerCellSystem _powerCell = default!;
    [Dependency] private SharedBatterySystem _battery = default!;

    private float _updateAccumulator;

    public override void Initialize()
    {
        SubscribeLocalEvent<ShowEnergyAlarmComponent, MapInitEvent>(OnMapInit);
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
                UpdatePowerAlert((uid, comp));
        }
    }

    private void OnMapInit(Entity<ShowEnergyAlarmComponent> ent, ref MapInitEvent args)
    {
        if (HasComp<ClothingComponent>(ent))
            return;

        ent.Comp.User = ent.Owner;
        UpdatePowerAlert(ent);
    }

    private void OnPowerCellUpdate(Entity<ShowEnergyAlarmComponent> ent, ref PowerCellChangedEvent args)
    {
        UpdatePowerAlert(ent);
    }

    private void OnEquipped(Entity<ShowEnergyAlarmComponent> ent, ref GotEquippedEvent args)
    {
        ent.Comp.User = args.EquipTarget;
        UpdatePowerAlert(ent);
    }

    private void OnUnequipped(Entity<ShowEnergyAlarmComponent> ent, ref GotUnequippedEvent args)
    {
        if (ent.Comp.User != null)
            ClearAlerts(ent, ent.Comp.User.Value);

        ent.Comp.User = null;
    }

    private void ClearAlerts(Entity<ShowEnergyAlarmComponent> ent, EntityUid user)
    {
        _alerts.ClearAlert(user, ent.Comp.PowerAlert);

        if (ent.Comp.NoBatteryAlert is { } noBatteryAlert)
            _alerts.ClearAlert(user, noBatteryAlert);
    }

    private void UpdatePowerAlert(Entity<ShowEnergyAlarmComponent> ent)
    {
        if (ent.Comp.User is not { } user)
            return;

        if (TryComp<PowerCellDrawComponent>(ent, out var draw) && !draw.Enabled)
        {
            ClearAlerts(ent, user);
            return;
        }

        if (!_powerCell.TryGetBatteryFromSlot(ent.Owner, out var battery))
        {
            _alerts.ClearAlert(user, ent.Comp.PowerAlert);

            if (ent.Comp.NoBatteryAlert is { } noBatteryAlert)
                _alerts.ShowAlert(user, noBatteryAlert);

            return;
        }

        if (ent.Comp.NoBatteryAlert is { } staleNoBatteryAlert)
            _alerts.ClearAlert(user, staleNoBatteryAlert);

        var minSeverity = _alerts.GetMinSeverity(ent.Comp.PowerAlert);
        var levels = _alerts.GetMaxSeverity(ent.Comp.PowerAlert) - minSeverity + 1;
        var charge = MathF.Max(0f, _battery.GetCharge(battery.Value.AsNullable()));
        var level = ContentHelpers.RoundToLevels(charge, battery.Value.Comp.MaxCharge, levels);
        _alerts.ShowAlert(user, ent.Comp.PowerAlert, (short) (minSeverity + level));
    }
}
