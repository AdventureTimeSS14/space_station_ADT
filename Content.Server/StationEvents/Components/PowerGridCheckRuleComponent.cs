using System.Threading;
using Content.Server.StationEvents.Events;
using Robust.Shared.Audio;

namespace Content.Server.StationEvents.Components;

[RegisterComponent, Access(typeof(PowerGridCheckRule))]
public sealed partial class PowerGridCheckRuleComponent : Component
{
    public CancellationTokenSource? AnnounceCancelToken;

    public EntityUid AffectedStation;
    public readonly List<EntityUid> Powered = new();
    public readonly List<EntityUid> Unpowered = new();

    public float SecondsUntilOff = 30.0f;

    public int NumberPerSecond = 0;
    public float UpdateRate => 1.0f / NumberPerSecond;
    public float FrameTimeAccumulator = 0.0f;

    // ADT Start
    [DataField]
    public SoundSpecifier? EndSound;
    // ADT End
}
