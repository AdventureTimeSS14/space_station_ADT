using Content.Shared.Body;
using Content.Shared.Damage.Prototypes;
using Content.Shared.Database;
using Content.Shared.EntityEffects;
using Content.Shared.Humanoid;
using Content.Shared.Humanoid.Markings;
using Content.Shared.Polymorph;
using Robust.Shared.Prototypes;

namespace Content.Shared.ADT.Drake.Loot;

public sealed partial class ADTDraconidTransformation : EntityEffectBase<ADTDraconidTransformation>
{
    [DataField(required: true)]
    public ProtoId<PolymorphPrototype> Polymorph;

    [DataField(required: true)]
    public ProtoId<PolymorphPrototype> TribePolymorph;

    [DataField]
    public List<ProtoId<DamageGroupPrototype>> HealedGroups = new() { "Brute", "Burn" };

    [DataField]
    public Color SkinColor = Color.FromHex("#000000");

    [DataField]
    public Color EyeColor = Color.FromHex("#A02720");

    [DataField]
    public ProtoId<OrganCategoryPrototype> HornsCategory = "Head";

    [DataField]
    public HumanoidVisualLayers HornsLayer = HumanoidVisualLayers.HeadTop;

    [DataField]
    public ProtoId<MarkingPrototype> HornsMarking = "LizardHornsDemonic";

    [DataField]
    public Color HornsColor = Color.FromHex("#000000");

    public override string? EntityEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
    {
        return null;
    }

    public override LogImpact? Impact => LogImpact.Medium;
}
