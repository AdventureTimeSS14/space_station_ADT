using Robust.Shared.Prototypes;

namespace Content.Shared.ADT.Achievements;

[Prototype("adtAchievementTrigger")]
public sealed partial class ADTAchievementTriggerPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;
}
