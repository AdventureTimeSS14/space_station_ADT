using Content.Shared.Chemistry.Reagent;
using Content.Shared.Damage;
using Content.Shared.FixedPoint;
using Robust.Shared.Prototypes;

namespace Content.Shared.ADT.Lavaland.Components;

[RegisterComponent]
public sealed partial class ADTAcidBladderComponent : Component
{
    [DataField]
    public ProtoId<ReagentPrototype> Reagent = "SulfuricAcid";

    [DataField]
    public FixedPoint2 MobAmount = 40;

    [DataField]
    public FixedPoint2 FloorAmount = 80;

    [DataField(required: true)]
    public DamageSpecifier WallDamage = default!;
}
