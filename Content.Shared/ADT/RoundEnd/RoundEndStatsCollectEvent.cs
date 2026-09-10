using Robust.Shared.GameObjects;

namespace Content.Shared.ADT.RoundEnd;

[ByRefEvent]
public sealed class RoundEndStatsCollectEvent
{
    public List<RoundEndStatEntry> Entries = new();
    public Dictionary<string, int> SpeciesCensus = new();

    public RoundEndStatEntry Add(RoundEndStatCategory category, string locId, int order = 0)
    {
        var entry = new RoundEndStatEntry(category, locId, order);
        Entries.Add(entry);
        return entry;
    }
}
