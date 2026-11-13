using Content.Shared.Body;
using Content.Shared.Inventory;
using Robust.Shared.Prototypes;

namespace Content.Shared.ADT.Body.Bones;

[RegisterComponent]
public sealed partial class ADTBonesComponent : Component
{
    [DataField]
    public bool CanBreak = true;

    [DataField]
    public float Fragility = 1f;

    [DataField]
    public float AccumulatedFactor = 0.5f;

    [DataField]
    public float ExplosionLimbFactor = 0.35f;

    [DataField]
    public float MinBreakDamage = 10f;

    [DataField]
    public TimeSpan IgnoreDuration = TimeSpan.FromSeconds(10);

    [DataField]
    public float ReleaseRestraintsChance = 0.25f;

    [DataField]
    public float ScreamOnHitChance = 0.4f;

    [DataField]
    public float SplintBreakChancePerDamage = 0.04f;

    [DataField]
    public TimeSpan SplintBreakStun = TimeSpan.FromSeconds(4);

    [DataField]
    public TimeSpan FeedbackCooldown = TimeSpan.FromSeconds(2);

    [ViewVariables]
    public TimeSpan NextBreakFeedback;

    [ViewVariables]
    public TimeSpan NextScream;

    [DataField]
    public Dictionary<ProtoId<OrganCategoryPrototype>, SlotFlags> Coverage = new()
    {
        ["Head"] = SlotFlags.HEAD,
        ["Torso"] = SlotFlags.OUTERCLOTHING | SlotFlags.INNERCLOTHING,
        ["ArmLeft"] = SlotFlags.OUTERCLOTHING | SlotFlags.INNERCLOTHING,
        ["ArmRight"] = SlotFlags.OUTERCLOTHING | SlotFlags.INNERCLOTHING,
        ["LegLeft"] = SlotFlags.OUTERCLOTHING | SlotFlags.INNERCLOTHING,
        ["LegRight"] = SlotFlags.OUTERCLOTHING | SlotFlags.INNERCLOTHING,
        ["HandLeft"] = SlotFlags.GLOVES | SlotFlags.OUTERCLOTHING,
        ["HandRight"] = SlotFlags.GLOVES | SlotFlags.OUTERCLOTHING,
        ["FootLeft"] = SlotFlags.FEET | SlotFlags.OUTERCLOTHING,
        ["FootRight"] = SlotFlags.FEET | SlotFlags.OUTERCLOTHING,
    };

    [ViewVariables]
    public float PendingSourceBrute;

    [ViewVariables]
    public ProtoId<OrganCategoryPrototype>? PendingZone;
}
