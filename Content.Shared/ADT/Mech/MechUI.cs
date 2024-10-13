using Content.Shared.Mech;
using Robust.Shared.Serialization;

namespace Content.Shared.ADT.Mech;

[Serializable, NetSerializable]
public sealed class MechGunUiState : BoundUserInterfaceState
{
    public int Capacity;
    public int Shots;
    public float ReloadTime;
}

[Serializable, NetSerializable]
public sealed class MechGunReloadMessage : MechEquipmentUiMessage
{

    public MechGunReloadMessage(NetEntity equipment)
    {
        Equipment = equipment;
    }
}
