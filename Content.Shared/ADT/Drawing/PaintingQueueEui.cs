using Content.Shared.Eui;
using Robust.Shared.Serialization;

namespace Content.Shared.ADT.Drawing;

[Serializable, NetSerializable]
public enum PaintingQueueFilter : byte
{
    Submitted,
    Approved,
    Hidden,
    Rejected,
}

[Serializable, NetSerializable]
public sealed class PaintingQueueEntry
{
    public Guid PaintingUid;
    public string Title = string.Empty;
    public string Description = string.Empty;
    public int Width;
    public int Height;
    public PaintingStatus Status;
    public Guid OwnerId;
    public string OwnerName = string.Empty;
    public DateTime? SubmittedAt;
    public DateTime? ReviewedAt;
    public string? ReviewerName;
    public string? ReviewComment;
    public int? RoundId;
    public long Bytes;
    public bool Pinned;
    public List<string> Contributors = new();
    public int OwnerApproved;
    public int OwnerRejected;
    public bool OwnerBanned;
}

[Serializable, NetSerializable]
public sealed class PaintingQueueEuiState : EuiStateBase
{
    public PaintingQueueFilter Filter;
    public List<PaintingQueueEntry> Entries = new();
    public bool CanModerate;
    public bool CanDelete;
    public bool CanBan;
    public bool CanSpawn;
    public long ApprovedTotalBytes;
    public long ApprovedLimitBytes;
    public string Status = string.Empty;
}

public static class PaintingQueueEuiMsg
{
    [Serializable, NetSerializable]
    public sealed class Refresh : EuiMessageBase
    {
        public PaintingQueueFilter Filter;
    }

    [Serializable, NetSerializable]
    public sealed class Approve : EuiMessageBase
    {
        public Guid Painting;
    }

    [Serializable, NetSerializable]
    public sealed class Reject : EuiMessageBase
    {
        public Guid Painting;
        public string Reason = string.Empty;
    }

    [Serializable, NetSerializable]
    public sealed class SetHidden : EuiMessageBase
    {
        public Guid Painting;
        public bool Hidden;
    }

    [Serializable, NetSerializable]
    public sealed class Delete : EuiMessageBase
    {
        public Guid Painting;
    }

    [Serializable, NetSerializable]
    public sealed class SetPinned : EuiMessageBase
    {
        public Guid Painting;
        public bool Pinned;
    }

    [Serializable, NetSerializable]
    public sealed class Ban : EuiMessageBase
    {
        public Guid UserId;
        public int Minutes;
        public string Reason = string.Empty;
    }

    [Serializable, NetSerializable]
    public sealed class Unban : EuiMessageBase
    {
        public Guid UserId;
    }

    [Serializable, NetSerializable]
    public sealed class Spawn : EuiMessageBase
    {
        public Guid Painting;
    }
}
