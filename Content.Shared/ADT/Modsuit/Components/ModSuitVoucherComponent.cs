using Robust.Shared.Prototypes;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.ADT.ModSuits;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class ModSuitVoucherComponent : Component
{
    [DataField("modId", required: true)]
    public EntProtoId ModId = default!;

    [DataField("hardId", required: true)]
    public EntProtoId HardId = default!;

    [DataField, AutoNetworkedField]
    public SuitType Current = SuitType.MOD;
}

public enum SuitType : byte
{
    MOD = 0,
    Hard = 1
}

[NetSerializable, Serializable]
public enum SuitVoucherVisuals : byte
{
    State
}