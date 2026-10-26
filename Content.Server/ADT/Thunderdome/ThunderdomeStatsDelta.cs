namespace Content.Server.ADT.Thunderdome;

public sealed class ThunderdomeLeaderboardRow
{
    public Guid UserId;
    public string Name = string.Empty;
    public int Kills;
    public int Deaths;
    public float Score;
    public int BestStreak;
}

public sealed class ThunderdomeStatsDelta
{
    public Guid UserId;
    public int Kills;
    public int Deaths;
    public float Score;
    public int RoundsPlayed;
    public int BestStreak;
    public bool IsEmpty
    {
        get
        {
            return Kills == 0
                && Deaths == 0
                && Score == 0f
                && RoundsPlayed == 0
                && BestStreak == 0;
        }
    }
}
