using Content.Shared.Actions;
using Robust.Shared.Serialization;

namespace Content.Shared.ADT.Thunderdome;

/// <summary>
/// Ghost requests to join the thunderdome.
/// </summary>
[Serializable, NetSerializable]
public sealed partial class ThunderdomeJoinRequestEvent : EntityEventArgs;

/// <summary>
/// Server broadcasts the current thunderdome player count to ghosts.
/// </summary>
[Serializable, NetSerializable]
public sealed class ThunderdomePlayerCountEvent : EntityEventArgs
{
    public int Count { get; }

    public ThunderdomePlayerCountEvent(int count)
    {
        Count = count;
    }
}

/// <summary>
/// Server announces kill streaks / events to thunderdome participants.
/// </summary>
[Serializable, NetSerializable]
public sealed class ThunderdomeAnnouncementEvent : EntityEventArgs
{
    public string Message { get; }

    public ThunderdomeAnnouncementEvent(string message)
    {
        Message = message;
    }
}

/// <summary>
/// Server notifies a thunderdome player that their original station body has been revived.
/// </summary>
[Serializable, NetSerializable]
public sealed class ThunderdomeRevivalOfferEvent : EntityEventArgs;

/// <summary>
/// Client accepts the revival offer and wants to return to their original body.
/// </summary>
[Serializable, NetSerializable]
public sealed class ThunderdomeRevivalAcceptEvent : EntityEventArgs;

public sealed partial class ThunderdomeLeaveActionEvent : InstantActionEvent;
