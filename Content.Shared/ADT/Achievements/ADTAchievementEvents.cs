using Robust.Shared.Serialization;

namespace Content.Shared.ADT.Achievements;

[Serializable, NetSerializable]
public struct ADTAchievementState
{
    public int Progress;
    public bool Unlocked;
}

[Serializable, NetSerializable]
public sealed class ADTAchievementsRequestEvent : EntityEventArgs
{
}

[Serializable, NetSerializable]
public sealed class ADTAchievementsStateEvent : EntityEventArgs
{
    public Dictionary<string, ADTAchievementState> Achievements;

    public ADTAchievementsStateEvent(Dictionary<string, ADTAchievementState> achievements)
    {
        Achievements = achievements;
    }
}

[Serializable, NetSerializable]
public sealed class ADTAchievementUpdateEvent : EntityEventArgs
{
    public string Achievement;
    public ADTAchievementState State;

    public bool Announce;

    public ADTAchievementUpdateEvent(string achievement, ADTAchievementState state, bool announce)
    {
        Achievement = achievement;
        State = state;
        Announce = announce;
    }
}
