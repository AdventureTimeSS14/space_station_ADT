using Content.Shared.ADT.Silicon.Components;
using Content.Shared.Alert;
using Content.Shared.Power.EntitySystems;
using Content.Shared.PowerCell;
using Robust.Client.Player;
using Robust.Shared.Player;
using Robust.Shared.Timing;

namespace Content.Client.ADT.Silicon.Charge;

public sealed class SiliconChargeAlertSystem : EntitySystem
{
    [Dependency] private readonly PowerCellSystem _powerCell = default!;
    [Dependency] private readonly SharedBatterySystem _battery = default!;
    [Dependency] private readonly AlertsSystem _alerts = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IPlayerManager _player = default!;

    private static readonly TimeSpan AlertUpdateDelay = TimeSpan.FromSeconds(0.5f);

    private TimeSpan _nextAlertUpdate = TimeSpan.Zero;

    private EntityQuery<SiliconComponent> _siliconQuery;

    public override void Initialize()
    {
        base.Initialize();

        _siliconQuery = GetEntityQuery<SiliconComponent>();

        SubscribeLocalEvent<SiliconComponent, LocalPlayerAttachedEvent>(OnPlayerAttached);
        SubscribeLocalEvent<SiliconComponent, LocalPlayerDetachedEvent>(OnPlayerDetached);
        SubscribeLocalEvent<SiliconComponent, PowerCellChangedEvent>(OnPowerCellChanged);
        SubscribeLocalEvent<SiliconComponent, PowerCellSlotEmptyEvent>(OnPowerCellEmpty);
    }

    private void OnPlayerAttached(Entity<SiliconComponent> ent, ref LocalPlayerAttachedEvent args)
    {
        UpdateBatteryAlert(ent);
    }

    private void OnPlayerDetached(Entity<SiliconComponent> ent, ref LocalPlayerDetachedEvent args)
    {
        _alerts.ClearAlert(ent.Owner, ent.Comp.BatteryAlert);
        _alerts.ClearAlert(ent.Owner, ent.Comp.NoBatteryAlert);
    }

    private void OnPowerCellChanged(Entity<SiliconComponent> ent, ref PowerCellChangedEvent args)
    {
        if (_player.LocalEntity != ent.Owner)
            return;

        UpdateBatteryAlert(ent);
    }

    private void OnPowerCellEmpty(Entity<SiliconComponent> ent, ref PowerCellSlotEmptyEvent args)
    {
        if (_player.LocalEntity != ent.Owner)
            return;

        UpdateBatteryAlert(ent);
    }

    private void UpdateBatteryAlert(Entity<SiliconComponent> ent)
    {
        if (!ent.Comp.BatteryPowered)
            return;

        if (!_powerCell.TryGetBatteryFromSlot(ent.Owner, out var battery))
        {
            _alerts.ShowAlert(ent.Owner, ent.Comp.NoBatteryAlert);
            return;
        }

        var chargeLevel = (short)MathF.Round(_battery.GetChargeLevel(battery.Value.AsNullable()) * 10f);

        _alerts.ShowAlert(ent.Owner, ent.Comp.BatteryAlert, chargeLevel);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (_player.LocalEntity is not { } localPlayer)
            return;

        var curTime = _timing.CurTime;
        if (curTime < _nextAlertUpdate)
            return;

        _nextAlertUpdate = curTime + AlertUpdateDelay;

        if (!_siliconQuery.TryComp(localPlayer, out var silicon))
            return;

        UpdateBatteryAlert((localPlayer, silicon));
    }
}
