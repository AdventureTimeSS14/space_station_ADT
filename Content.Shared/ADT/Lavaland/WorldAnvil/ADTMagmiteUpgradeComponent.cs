using Robust.Shared.Prototypes;

namespace Content.Shared.ADT.Lavaland.WorldAnvil;

[RegisterComponent]
public sealed partial class ADTMagmiteUpgradeComponent : Component
{
}

[RegisterComponent]
public sealed partial class ADTMagmiteUpgradableComponent : Component
{
    [DataField(required: true)]
    public EntProtoId Result;
}
