using Robust.Shared.GameObjects;

namespace Content.Shared.ADT.Cyberpsychosis;

public sealed class CyberpsychosisStateChangedEvent : EntityEventArgs
{
    public readonly EntityUid Target;
    public readonly CyberpsychosisState OldState;
    public readonly CyberpsychosisState NewState;

    public CyberpsychosisStateChangedEvent(EntityUid target, CyberpsychosisState oldState, CyberpsychosisState newState)
    {
        Target = target;
        OldState = oldState;
        NewState = newState;
    }
}

public sealed class CyberpsychosisEpisodeStartedEvent : EntityEventArgs
{
    public readonly CyberpsychosisState State;
    public readonly TimeSpan Duration;

    public CyberpsychosisEpisodeStartedEvent(CyberpsychosisState state, TimeSpan duration)
    {
        State = state;
        Duration = duration;
    }
}

public sealed class CyberpsychosisEpisodeEndedEvent : EntityEventArgs { }
