using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Shared.ADT.Achievements;

[Prototype("adtAchievement")]
public sealed partial class ADTAchievementPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    [DataField(required: true)]
    public LocId Name;

    [DataField(required: true)]
    public LocId Description;

    [DataField(required: true)]
    public ProtoId<ADTAchievementCategoryPrototype> Category;

    [DataField]
    public SpriteSpecifier? Icon;

    [DataField]
    public int Goal = 1;

    [DataField]
    public bool Hidden;

    [DataField]
    public bool ShowProgress = true;

    [DataField]
    public int Points = 10;

    [DataField]
    public int Priority;

    [DataField(required: true)]
    public List<ADTAchievementCondition> Conditions = new();
}
