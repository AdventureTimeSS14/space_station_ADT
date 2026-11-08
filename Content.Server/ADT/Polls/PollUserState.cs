using Content.Shared.ADT.Polls;

namespace Content.Server.ADT.Polls;

public sealed class PollUserState
{
    public readonly HashSet<int> Answered = new();

    public readonly Dictionary<int, PollSnapshot> Pending = new();
}

public readonly record struct PollPendingEntry(int PollId, Guid UserId, PollSnapshot Snapshot);
