using Robust.Shared.Serialization;

namespace Content.Shared.ADT.Thunderdome;

[Serializable, NetSerializable]
public sealed partial class ThunderdomeLeaderboardEntry
{
    public int Rank { get; set; }
    public string Name { get; set; } = string.Empty;
    public float Score { get; set; }
    public int Kills { get; set; }
    public int Deaths { get; set; }
    public int BestStreak { get; set; }
    public bool IsSelf { get; set; }
}

[Serializable, NetSerializable]
public sealed partial class ThunderdomePersonalStats
{
    public int Rank { get; set; }

    public int TotalRanked { get; set; }

    public float Score { get; set; }
    public int Kills { get; set; }
    public int Deaths { get; set; }
    public int BestStreak { get; set; }
    public int RoundsPlayed { get; set; }
}

[Serializable, NetSerializable]
public sealed partial class ThunderdomeRoundStats
{
    public int Kills { get; set; }
    public int Deaths { get; set; }
    public int BestStreak { get; set; }
    public float Score { get; set; }

    public int Rank { get; set; }

    public int Participants { get; set; }

    public int DiscardedKills { get; set; }
}
