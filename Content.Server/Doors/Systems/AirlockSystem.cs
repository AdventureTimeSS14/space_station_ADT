using Content.Server.Power.Components;
using Content.Server.Shuttles.Components;
using Content.Server.Wires;
using Content.Shared.DeviceLinking.Events;
using Content.Shared.Doors.Components;
using Content.Shared.Doors.Systems;

namespace Content.Server.Doors.Systems;

public sealed partial class AirlockSystem : SharedAirlockSystem
{
    // ADT-Tweak-Start
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<AirlockComponent, ComponentInit>(OnAirlockInit);
    }

    private void OnAirlockInit(EntityUid uid, AirlockComponent component, ComponentInit args)
    {
        if (HasComp<ApcPowerReceiverComponent>(uid))
            Appearance.SetData(uid, DoorVisuals.ClosedLights, true); // Corvax-Resprite-Airlocks
    }

    protected override void OnPoweredADT(Entity<AirlockComponent> ent, DoorComponent door)
    {
        if (!TryComp<DockingComponent>(ent, out var docking) ||
            !docking.Docked ||
            door.State != DoorState.Closed)
            return;

        if (DoorSystem.TryOpen(ent, door) &&
            TryComp<DoorBoltComponent>(ent, out var doorBolt))
        {
            DoorSystem.SetBoltsDown((ent, doorBolt), true);
        }
    }
    // ADT-Tweak-End
}
