using Content.Shared.Mining;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Shared.ADT.Mining.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class ADTMineralOreComponent : Component
{
    [DataField]
    public ProtoId<OrePrototype>? Ore;

    [DataField]
    public SpriteSpecifier.Rsi? ScanOverlay;

    [DataField]
    public bool NoExplosionBonus;

    [DataField]
    public int FixedAmount;
}
