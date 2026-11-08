using Content.Server.ADT.VendingMachines; // ADT-Tweak
using Content.Server.VendingMachines.Components;
using Content.Server.Wires;
using Content.Shared.ADT.VendingMachines; // ADT-Tweak
using Content.Shared.VendingMachines.Components;
using Content.Shared.Wires;
using VendingMachineComponent = Content.Shared.ADT.VendingMachines.ADTVendingMachineComponent; // ADT-Tweak

namespace Content.Server.VendingMachines;

public sealed partial class VendingMachineEjectItemWireAction : ComponentWireAction<VendingMachineComponent>
{
    private Content.Server.ADT.VendingMachines.VendingMachineSystem _vendingMachineSystem = default!; // ADT-Tweak

    public override Color Color { get; set; } = Color.Red;
    public override string Name { get; set; } = "wire-name-vending-eject";

    public override object StatusKey => EjectWireKey.StatusKey;

    public override StatusLightState? GetLightState(Wire wire, VendingMachineComponent comp)
    {
        if (!EntityManager.HasComponent<VendingMachineEjectComponent>(wire.Owner))
            return StatusLightState.Off;

        return EntityManager.HasComponent<VendingMachineShootComponent>(wire.Owner)
            ? StatusLightState.BlinkingFast
            : StatusLightState.On;
    }

    public override void Initialize()
    {
        base.Initialize();

        _vendingMachineSystem = EntityManager.System<Content.Server.ADT.VendingMachines.VendingMachineSystem>(); // ADT-Tweak
    }

    public override bool Cut(EntityUid user, Wire wire, VendingMachineComponent vending)
    {
        _vendingMachineSystem.SetShooting(wire.Owner, true);
        return true;
    }

    public override bool Mend(EntityUid user, Wire wire, VendingMachineComponent vending)
    {
        _vendingMachineSystem.SetShooting(wire.Owner, false);
        return true;
    }

    public override void Pulse(EntityUid user, Wire wire, VendingMachineComponent vending)
    {
        _vendingMachineSystem.EjectRandom((wire.Owner, vending), true);
    }
}
