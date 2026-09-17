using Robust.Shared.Prototypes;

namespace Content.Shared.ADT.Lavaland.WorldAnvil;

[RegisterComponent]
public sealed partial class ADTMagmiteUpgradeComponent : Component
{
    [DataField]
    public float UpgradeDelay = 5f;

    [DataField]
    public LocId CoolMessage = "adt-magmite-parts-cooled";

    [DataField]
    public EntProtoId? CooledPrototype;
}

[RegisterComponent]
public sealed partial class ADTMagmiteUpgradableComponent : Component
{
    [DataField(required: true)]
    public EntProtoId Result;
}
