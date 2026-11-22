using Robust.Shared.Serialization;

namespace Content.Shared.ADT.Polls;

[Serializable, NetSerializable]
public sealed class PollListEvent : EntityEventArgs
{
    public List<PollSummary> Polls = new();
}

[Serializable, NetSerializable]
public sealed class PollInviteEvent : EntityEventArgs
{
    public List<int> PollIds = new();
}

[Serializable, NetSerializable]
public sealed class PollRequestEvent : EntityEventArgs
{
    public int PollId;
}

[Serializable, NetSerializable]
public sealed class PollContentEvent : EntityEventArgs
{
    public PollData Poll = new();

    public bool Answered;
}

[Serializable, NetSerializable]
public sealed class PollSubmitEvent : EntityEventArgs
{
    public int PollId;

    public List<PollAnswer> Answers = new();
}

[Serializable, NetSerializable]
public sealed class PollSubmitResultEvent : EntityEventArgs
{
    public int PollId;

    public bool Success;

    public string Message = string.Empty;
}
