using Content.Shared.Mech.Components;
using Content.Shared.Vehicle.Components;

namespace Content.Server.NPC.Systems;

public sealed partial class NPCUtilitySystem
{
    private bool IsActiveMech(EntityUid uid)
    {
        return TryComp<MechComponent>(uid, out var mech) &&
               !mech.Broken &&
               TryComp<VehicleComponent>(uid, out var vehicle) &&
               vehicle.Operator != null;
    }
}
