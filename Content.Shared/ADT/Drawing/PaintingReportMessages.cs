using Robust.Shared.Serialization;

namespace Content.Shared.ADT.Drawing;

[Serializable, NetSerializable]
public sealed class PaintingReportEntry
{
    public int ReportId;
    public Guid PaintingUid;
    public string Title = string.Empty;
    public PaintingStatus? Status;
    public Guid? OwnerId;
    public string OwnerName = string.Empty;
    public bool OwnerBanned;
    public string ReporterName = string.Empty;
    public string Reason = string.Empty;
    public DateTime CreatedAt;
    public int? RoundId;
    public int OpenReports;
    public string? ResolverName;
    public DateTime? ResolvedAt;
}

[Serializable, NetSerializable]
public sealed class PaintingReportsRequestEvent : EntityEventArgs
{
    public bool Resolved;
}

[Serializable, NetSerializable]
public sealed class PaintingReportsStateEvent : EntityEventArgs
{
    public bool Resolved;
    public bool CanBan;
    public List<PaintingReportEntry> Entries = new();
}

[Serializable, NetSerializable]
public sealed class PaintingReportsChangedEvent : EntityEventArgs
{
}

[Serializable, NetSerializable]
public sealed class PaintingReportsStatusEvent : EntityEventArgs
{
    public string Status = string.Empty;
}

[Serializable, NetSerializable]
public sealed class PaintingReportResolveEvent : EntityEventArgs
{
    public int ReportId;
}

[Serializable, NetSerializable]
public sealed class PaintingReportHideEvent : EntityEventArgs
{
    public int ReportId;
}

[Serializable, NetSerializable]
public sealed class PaintingReportBanEvent : EntityEventArgs
{
    public int ReportId;
    public int Minutes;
    public string Reason = string.Empty;
}
