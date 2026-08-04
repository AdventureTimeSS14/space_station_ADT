using Content.Shared.ADT.Achievements;
using Robust.Shared.Prototypes;

namespace Content.Shared.ADT.Achievements.Components;

[RegisterComponent]
public sealed partial class ADTAchievementSourceComponent : Component
{
    [DataField]
    public List<ProtoId<ADTAchievementTriggerPrototype>> OnDeath = new();

    [DataField]
    public List<ProtoId<ADTAchievementTriggerPrototype>> OnGathered = new();

    [DataField]
    public List<ProtoId<ADTAchievementTriggerPrototype>> OnHarvested = new();

    [DataField]
    public List<ProtoId<ADTAchievementTriggerPrototype>> OnDestroyed = new();

    [ViewVariables]
    public EntityUid? LastAttacker;
}
