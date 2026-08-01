using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.ADT.Weapons.Ranged.Upgrades.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class ADTGunUpgradeTracerComponent : Component
{
    [DataField, AutoNetworkedField]
    public Color BoltColor = Color.White;

    [DataField]
    public bool Adjustable;
}

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(raiseAfterAutoHandleState: true)]
public sealed partial class ADTProjectileTracerComponent : Component
{
    [DataField, AutoNetworkedField]
    public Color BoltColor = Color.White;
}

[RegisterComponent, NetworkedComponent]
public sealed partial class ADTGunUpgradeChassisComponent : Component
{
    [DataField]
    public string ChassisId = "super";

    [DataField]
    public Color ChassisColor = Color.Yellow;

    [DataField]
    public LocId ChassisName = string.Empty;
}

[RegisterComponent]
public sealed partial class ADTChassisPaintedComponent : Component
{
    [DataField]
    public string? OriginalName;
}

[Serializable, NetSerializable]
public enum ADTGunChassisVisuals : byte
{
    Chassis,
    Color,
}

[Serializable, NetSerializable]
public enum ADTGunUpgradeTracerUiKey : byte
{
    Key,
}

[Serializable, NetSerializable]
public sealed class ADTGunUpgradeTracerColorMessage(Color color) : BoundUserInterfaceMessage
{
    public readonly Color Color = color;
}
