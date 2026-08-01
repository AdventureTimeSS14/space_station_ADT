namespace Content.Server.ADT.Achievements;

public sealed class ADTAchievementRow
{
    public string AchievementId = default!;
    public int Progress;
    public bool Unlocked;
}

public sealed class ADTAchievementSave
{
    public Guid UserId;
    public string AchievementId = default!;
    public int Progress;
    public bool Unlocked;
}
