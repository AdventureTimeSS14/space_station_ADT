using Content.Shared.Whitelist;
using Robust.Shared.Prototypes;

namespace Content.Shared.ADT.AshFlora;

[RegisterComponent]
public sealed partial class ADTAshFloraComponent : Component
{
    [DataField]
    public EntProtoId? Product;

    [DataField]
    public int MinAmount = 1;

    [DataField]
    public int MaxAmount = 3;

    [DataField]
    public TimeSpan HarvestTime = TimeSpan.FromSeconds(6);

    [DataField]
    public EntityWhitelist? ToolWhitelist;

    [DataField]
    public EntProtoId? HarvestedPrototype;

    [DataField]
    public float ScatterOffset = 0.25f;

    [DataField]
    public LocId PopupStart = "adt-ash-flora-harvest-start";

    [DataField]
    public LocId PopupLow = "adt-ash-flora-harvest-low";

    [DataField]
    public LocId PopupMedium = "adt-ash-flora-harvest-medium";

    [DataField]
    public LocId PopupHigh = "adt-ash-flora-harvest-high";
}
