using Robust.Shared.Serialization;

namespace Content.Shared.ADT.RoundEnd;

[Serializable, NetSerializable]
public enum RoundEndStatCategory : byte
{
    Summary,
    FirstDeath,
    Economy,
    Misc,
    Census,
}

[Serializable, NetSerializable]
public sealed class RoundEndStatEntry
{
    public const string YesLocId = "round-end-report-yes";
    public const string NoLocId = "round-end-report-no";

    public RoundEndStatCategory Category;
    public string LocId = string.Empty;
    public Dictionary<string, string> Args = new();
    public Dictionary<string, string> LocArgs = new();
    public int Order;

    public RoundEndStatEntry()
    {
    }

    public RoundEndStatEntry(RoundEndStatCategory category, string locId, int order = 0)
    {
        Category = category;
        LocId = locId;
        Order = order;
    }

    public RoundEndStatEntry WithArg(string key, string value)
    {
        Args[key] = value;
        return this;
    }

    public RoundEndStatEntry WithArg(string key, int value)
    {
        Args[key] = value.ToString();
        return this;
    }

    public RoundEndStatEntry WithRate(string key, int value, int total)
    {
        Args[key] = value.ToString();
        Args[key + "Percent"] = total <= 0 ? "0" : (value * 100 / total).ToString();
        return this;
    }

    public RoundEndStatEntry WithLocArg(string key, string locId)
    {
        LocArgs[key] = locId;
        return this;
    }

    public RoundEndStatEntry WithBool(string key, bool value)
    {
        return WithLocArg(key, value ? YesLocId : NoLocId);
    }
}
