using Content.Server.ADT.Chaplain.Components;
using Content.Server.Atmos.Components;
using Content.Server.Atmos.EntitySystems;
using Content.Shared.ADT.Chaplain.Components;
using Content.Shared.ADT.Chaplain.Systems;
using Content.Shared.Inventory;
using Content.Shared.Inventory.Events;
using Content.Shared.Temperature;
using Robust.Shared.Containers;

namespace Content.Server.ADT.Chaplain.Systems;

public sealed class HolySpaceProtectionSystem : EntitySystem
{
    [Dependency] private BarotraumaSystem _barotrauma = default!;
    [Dependency] private ForTheWorthySystem _worthy = default!;
    [Dependency] private SharedContainerSystem _container = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<HolySpaceProtectionComponent, GetPressureProtectionValuesEvent>(OnGetPressureProtection);
        SubscribeLocalEvent<HolySpaceProtectionComponent, InventoryRelayedEvent<BeforeHeatExchangeEvent>>(OnBeforeHeatExchange);
        SubscribeLocalEvent<HolyRelicComponent, GotEquippedEvent>(OnRelicEquipped);
        SubscribeLocalEvent<HolyRelicComponent, GotUnequippedEvent>(OnRelicUnequipped);
    }

    private void OnGetPressureProtection(Entity<HolySpaceProtectionComponent> ent, ref GetPressureProtectionValuesEvent args)
    {
        if (_container.TryGetContainingContainer((ent.Owner, null, null), out var container) && IsWorthy(ent, container.Owner))
            return;

        args.HighPressureMultiplier = 1f;
        args.HighPressureModifier = 0f;
        args.LowPressureMultiplier = 1f;
        args.LowPressureModifier = 0f;
    }

    private void OnBeforeHeatExchange(Entity<HolySpaceProtectionComponent> ent, ref InventoryRelayedEvent<BeforeHeatExchangeEvent> args)
    {
        if (IsWorthy(ent, args.Owner))
            args.Args.HeatTransferModifier *= ent.Comp.HeatCoefficient;
    }

    private void OnRelicEquipped(Entity<HolyRelicComponent> ent, ref GotEquippedEvent args)
    {
        _barotrauma.RefreshPressureResistance(args.EquipTarget);
    }

    private void OnRelicUnequipped(Entity<HolyRelicComponent> ent, ref GotUnequippedEvent args)
    {
        _barotrauma.RefreshPressureResistance(args.EquipTarget);
    }

    private bool IsWorthy(EntityUid item, EntityUid wearer)
    {
        return TryComp<ForTheWorthyComponent>(item, out var worthy) && _worthy.IsWorthy((item, worthy), wearer);
    }
}
