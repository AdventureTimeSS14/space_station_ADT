using Content.Shared.Damage;
using Content.Shared.FixedPoint;
using Robust.Shared.Prototypes;

namespace Content.Shared.ADT.AshWalker.Components;

[RegisterComponent]
public sealed partial class ADTAshWalkerNestComponent : Component
{
    [DataField]
    public int MeatCounter = 6;

    [DataField]
    public int MeatPerEgg = 2;

    [DataField]
    public int MeatPerBody = 1;

    [DataField]
    public float ConsumeRange = 1.5f;

    [DataField]
    public float HealRange = 4f;

    [DataField]
    public FixedPoint2 SelfRepair = 10;

    [DataField]
    public DamageSpecifier AuraHealing = new();

    [DataField]
    public FixedPoint2 AuraBloodRestore = 5;

    [DataField]
    public EntProtoId Egg = "ADTAshWalkerEgg";

    [DataField]
    public EntProtoId ShamanEgg = "ADTAshWalkerShamanEgg";

    [DataField]
    public EntProtoId? LimbDrop;

    [DataField]
    public float LimbDropChance;

    [DataField]
    public List<EntProtoId> OrganDrops = new();

    [DataField]
    public TimeSpan Interval = TimeSpan.FromSeconds(1);

    [ViewVariables]
    public TimeSpan NextUpdate;
}
