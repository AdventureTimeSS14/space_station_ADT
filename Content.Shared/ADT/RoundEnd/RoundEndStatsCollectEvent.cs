using Robust.Shared.GameObjects;

namespace Content.Shared.ADT.RoundEnd;

/// <summary>
///     Raised on the server right before the round end message is built,
///     so systems can contribute ss13-style round statistics.
/// </summary>
[ByRefEvent]
public sealed class RoundEndStatsCollectEvent
{
    public Dictionary<string, int> Stats = new();

    public Dictionary<string, int> SpeciesCensus = new();

    public string? RichestEscapedName;

    public string? RichestEscapedJob;

    public int RichestEscapedBalance;
}
