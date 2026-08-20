using Content.Shared.ADT.Xenobiology.XenobiologyBountyConsole;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared.ADT.Xenobiology.XenobiologyBountyConsole;

/// <summary>
/// Stores all active cargo bounties for a particular station.
/// </summary>
[RegisterComponent]
public sealed partial class StationXenobiologyBountyDatabaseComponent : Component
{
    /// <summary>
    /// A list of all the bounties currently active for a station.
    /// </summary>
    [DataField]
    public List<XenobiologyBountyData> Bounties = [];

    /// <summary>
    /// A list of all the bounties that have been completed or
    /// skipped for a station.
    /// </summary>
    [DataField]
    public List<XenobiologyBountyHistoryData> History = [];

    /// <summary>
    /// The time at which players will be able to skip the next bounty.
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    public TimeSpan NextSkipTime = TimeSpan.Zero;

    /// <summary>
    /// The time between skipping bounties.
    /// </summary>
    [DataField]
    public TimeSpan SkipDelay = TimeSpan.FromMinutes(10);

    /// <summary>
    /// Maximum number of active bounties allowed at once.
    /// </summary>
    [DataField]
    public int MaxBounties = 6;

    /// <summary>
    /// The time between global bounty refreshes.
    /// </summary>
    [DataField]
    public TimeSpan GlobalMarketRefreshDelay = TimeSpan.FromMinutes(12);

    /// <summary>
    /// The time at which all bounties will refresh.
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    public TimeSpan NextGlobalMarketRefresh = TimeSpan.Zero;
}
