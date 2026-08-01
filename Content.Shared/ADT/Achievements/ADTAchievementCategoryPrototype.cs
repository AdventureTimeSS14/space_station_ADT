using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Shared.ADT.Achievements;

[Prototype("adtAchievementCategory")]
public sealed partial class ADTAchievementCategoryPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    [DataField(required: true)]
    public LocId Name;

    [DataField]
    public int Priority;

    [DataField]
    public SpriteSpecifier? Icon;
}
