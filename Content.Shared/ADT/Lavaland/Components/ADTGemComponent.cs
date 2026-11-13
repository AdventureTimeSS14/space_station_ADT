using Robust.Shared.Prototypes;

namespace Content.Shared.ADT.Lavaland.Components;

[RegisterComponent]
public sealed partial class ADTGemComponent : Component
{
    [DataField]
    public uint PointValue = 100;

    [DataField]
    public EntProtoId? Sheet;

    [DataField]
    public int SheetAmount;

    [DataField]
    public float WeldDelay = 1f;

    [DataField]
    public string? JewelryKey;

    [ViewVariables]
    public bool Analysed;
}
