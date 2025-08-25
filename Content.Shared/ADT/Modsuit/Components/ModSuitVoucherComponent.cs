using Robust.Shared.Prototypes;
using Robust.Shared.GameStates;

namespace Content.Shared.ADT.ModSuits;

[RegisterComponent, NetworkedComponent]
public sealed partial class ModSuitVoucherComponent : Component
{
    [DataField("modId", required: true)]
    public EntProtoId ModId = default!;

    [DataField("hardId", required: true)]
    public EntProtoId HardId = default!;

    public ModSuitType Current = ModSuitType.MOD;

    [DataField]
    public string State = string.Empty;
}

public enum ModSuitType : byte
{
    MOD,
    Hard
}