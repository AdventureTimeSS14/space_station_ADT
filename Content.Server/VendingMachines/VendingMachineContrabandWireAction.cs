using Content.Server.ADT.VendingMachines; // ADT-Tweak
using Content.Server.Wires;
using Content.Shared.ADT.VendingMachines; // ADT-Tweak
using VendingMachineComponent = Content.Shared.ADT.VendingMachines.ADTVendingMachineComponent; // ADT-Tweak
using Content.Shared.Wires;

namespace Content.Server.VendingMachines;

[DataDefinition]
public sealed partial class VendingMachineContrabandWireAction : BaseToggleWireAction
{
    private Content.Server.ADT.VendingMachines.VendingMachineSystem _vendingMachineSystem = default!; // ADT-Tweak

    public override Color Color { get; set; } = Color.Green;
    public override string Name { get; set; } = "wire-name-vending-contraband";
    public override object StatusKey => ContrabandWireKey.StatusKey;
    public override object TimeoutKey => ContrabandWireKey.TimeoutKey;

    public override void Initialize()
    {
        base.Initialize();

        _vendingMachineSystem = EntityManager.System<Content.Server.ADT.VendingMachines.VendingMachineSystem>(); // ADT-Tweak
    }

    public override StatusLightState? GetLightState(Wire wire)
    {
        if (EntityManager.TryGetComponent(wire.Owner, out VendingMachineComponent? vending))
        {
            return vending.Contraband
                ? StatusLightState.BlinkingSlow
                : StatusLightState.On;
        }

        return StatusLightState.Off;
    }

    public override void ToggleValue(EntityUid owner, bool setting)
    {
        if (EntityManager.TryGetComponent(owner, out VendingMachineComponent? vending))
        {
            _vendingMachineSystem.SetContraband((owner, vending), !vending.Contraband);
        }
    }

    public override bool GetValue(EntityUid owner)
    {
        return EntityManager.TryGetComponent(owner, out VendingMachineComponent? vending) && !vending.Contraband;
    }
}
