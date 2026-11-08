using Robust.Shared.Prototypes;

namespace Content.Shared.ADT.Lavaland.Components;

[RegisterComponent]
public sealed partial class ADTGemJewelryComponent : Component
{
    [DataField]
    public string Slot = "gem";

    [DataField(required: true)]
    public string NamePrefix = string.Empty;

    [DataField(required: true)]
    public string EquippedSlot = string.Empty;

    [DataField]
    public Dictionary<string, EntProtoId> KeyEffects = new();

    [ViewVariables]
    public string? Key;

    [ViewVariables]
    public EntityUid? Wearer;

    [ViewVariables]
    public string? BaseName;
}
