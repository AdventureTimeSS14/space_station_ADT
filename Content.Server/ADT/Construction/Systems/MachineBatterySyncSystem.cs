using Content.Server.Construction.Components;
using Content.Server.Power.EntitySystems;
using Content.Shared.Power.Components;
using Robust.Shared.Containers;

namespace Content.Server.ADT.Construction.Systems;

public sealed class MachineBatterySyncSystem : EntitySystem
{
    [Dependency] private readonly BatterySystem _battery = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MachineComponent, EntRemovedFromContainerMessage>(OnEntRemovedFromParts);
    }

    private void OnEntRemovedFromParts(Entity<MachineComponent> ent, ref EntRemovedFromContainerMessage args)
    {
        if (args.Container.ID != MachineFrameComponent.PartContainerName
            || !TryComp<BatteryComponent>(args.Entity, out var itemBattery)
            || !TryComp<BatteryComponent>(ent, out var battery))
            return;

        var percent = battery.MaxCharge <= 0f ? 0f : _battery.GetCharge((ent, battery)) / battery.MaxCharge;
        _battery.SetCharge((args.Entity, itemBattery), itemBattery.MaxCharge * percent);
    }
}