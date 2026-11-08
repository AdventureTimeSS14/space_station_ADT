// Ported from RMC-14 (https://github.com/RMC-14/RMC-14), MIT License
using Content.Shared._RMC14.Line;
using Content.Shared.Chemistry.Reagent;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Weapons.Ranged.Flamer;

[RegisterComponent]
public sealed partial class RMCFlamerChainComponent : Component
{
    [DataField]
    public EntProtoId Spawn = "RMCTileFire";

    [DataField]
    public List<LineTile> Tiles = new();

    [DataField]
    public EntityUid? Chain;

    [DataField]
    public ProtoId<ReagentPrototype> Reagent = "RMCNapalmUT";

    [DataField]
    public int MaxIntensity = 20;

    [DataField]
    public int MaxDuration = 24;

    [DataField]
    public int FuelPressure = 1;
}
