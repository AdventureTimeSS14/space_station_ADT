using Content.Shared.Eui;
using Robust.Shared.Serialization;

namespace Content.Shared.ADT.Polls;

[Serializable, NetSerializable]
public enum PollGroupBy : byte
{
    None,
    Department,
    Job,
    Species,
}

[Serializable, NetSerializable]
public sealed class PollAdminEntry
{
    public PollData Poll = new();

    public int Responses;

    public int Pending;
}

[Serializable, NetSerializable]
public sealed class PollQuestionStats
{
    public int Answered;

    public int[] Counts = Array.Empty<int>();

    public Dictionary<string, int[]> Groups = new();
}

[Serializable, NetSerializable]
public sealed class PollResponseView
{
    public Guid UserId;

    public string UserName = string.Empty;

    public DateTime SubmittedAt;

    public int? RoundId;

    public string Server = string.Empty;

    public bool Targeted;

    public PollSnapshot Snapshot = new();

    public List<PollAnswer> Answers = new();
}

[Serializable, NetSerializable]
public sealed class PollStats
{
    public int PollId;

    public int Total;

    public int Targeted;

    public PollGroupBy GroupBy;

    public List<PollQuestionStats> Questions = new();

    public List<PollResponseView> Page = new();

    public int PageIndex;

    public int PageCount;
}

[Serializable, NetSerializable]
public sealed class PollPanelEuiState : EuiStateBase
{
    public bool HasPermission;

    public string ServerName = string.Empty;

    public List<PollAdminEntry> Polls = new();

    public PollStats? Stats;

    public string Status = string.Empty;
}

public static class PollPanelEuiMsg
{
    [Serializable, NetSerializable]
    public sealed class Refresh : EuiMessageBase
    {
    }

    [Serializable, NetSerializable]
    public sealed class Save : EuiMessageBase
    {
        public PollData Poll = new();
    }

    [Serializable, NetSerializable]
    public sealed class SetClosed : EuiMessageBase
    {
        public int PollId;

        public bool Closed;
    }

    [Serializable, NetSerializable]
    public sealed class Delete : EuiMessageBase
    {
        public int PollId;
    }

    [Serializable, NetSerializable]
    public sealed class Wipe : EuiMessageBase
    {
        public int PollId;
    }

    [Serializable, NetSerializable]
    public sealed class RequestStats : EuiMessageBase
    {
        public int PollId;

        public PollGroupBy GroupBy;

        public int Page;
    }
}
