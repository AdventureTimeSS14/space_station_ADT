using Robust.Shared.Audio;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared.ADT.Xenobiology.XenobiologyBountyConsole;

[RegisterComponent, AutoGenerateComponentPause]
public sealed partial class XenobiologyBountyConsoleComponent : Component
{
    [DataField]
    public SoundSpecifier FulfillSound = new SoundPathSpecifier("/Audio/Effects/Cargo/ping.ogg");

    [DataField]
    public SoundSpecifier DenySound = new SoundPathSpecifier("/Audio/Effects/Cargo/buzz_two.ogg");

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoPausedField]
    public TimeSpan NextDenySoundTime = TimeSpan.Zero;

    [DataField]
    public TimeSpan DenySoundDelay = TimeSpan.FromSeconds(2);
}

[NetSerializable, Serializable]
public sealed class XenobiologyBountyConsoleState(
    List<XenobiologyBountyData> bounties,
    List<XenobiologyBountyHistoryData> history,
    TimeSpan untilNextSkip,
    TimeSpan untilNextGlobalRefresh)
    : BoundUserInterfaceState
{
    public List<XenobiologyBountyData> Bounties = bounties;
    public List<XenobiologyBountyHistoryData> History = history;
    public TimeSpan UntilNextSkip = untilNextSkip;
    public TimeSpan UntilNextGlobalRefresh = untilNextGlobalRefresh;
}

[Serializable, NetSerializable]
public sealed class BountyFulfillMessage(string bountyId) : BoundUserInterfaceMessage
{
    public string BountyId = bountyId;
}
