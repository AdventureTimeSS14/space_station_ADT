using Content.Shared.Whitelist;
using Robust.Shared.Prototypes;

namespace Content.Shared.ADT.Achievements;

[ImplicitDataDefinitionForInheritors]
public abstract partial class ADTAchievementCondition
{
    [DataField(required: true)]
    public List<ProtoId<ADTAchievementTriggerPrototype>> Triggers = new();

    public abstract int GetProgress(in ADTAchievementConditionArgs args);
}

public readonly record struct ADTAchievementConditionArgs(
    ProtoId<ADTAchievementTriggerPrototype> Trigger,
    EntityUid? Target,
    string? Key,
    int Amount,
    IEntityManager EntityManager,
    EntityWhitelistSystem Whitelist);

public sealed partial class ADTTriggerCondition : ADTAchievementCondition
{
    [DataField]
    public EntityWhitelist? Whitelist;

    [DataField]
    public EntityWhitelist? Blacklist;

    [DataField]
    public int Multiplier = 1;

    public override int GetProgress(in ADTAchievementConditionArgs args)
    {
        if (args.Target is not { } target)
            return Whitelist == null && Blacklist == null ? args.Amount * Multiplier : 0;

        if (Whitelist != null && args.Whitelist.IsWhitelistFail(Whitelist, target))
            return 0;

        if (Blacklist != null && args.Whitelist.IsWhitelistPass(Blacklist, target))
            return 0;

        return args.Amount * Multiplier;
    }
}

public sealed partial class ADTAchievementUnlockedCondition : ADTAchievementCondition
{
    [DataField]
    public List<ProtoId<ADTAchievementPrototype>> Achievements = new();

    public override int GetProgress(in ADTAchievementConditionArgs args)
    {
        if (args.Key == null)
            return 0;

        if (Achievements.Count == 0)
            return args.Amount;

        foreach (var achievement in Achievements)
        {
            if (achievement.Id == args.Key)
                return args.Amount;
        }

        return 0;
    }
}
