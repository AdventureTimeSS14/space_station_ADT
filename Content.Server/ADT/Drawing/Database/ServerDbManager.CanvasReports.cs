using System.Threading.Tasks;
using Content.Server.ADT.Drawing.Database;

namespace Content.Server.Database;

public partial interface IServerDbManager
{
    Task<bool> HasOpenPaintingReportAsync(Guid paintingUid, Guid reporter);
    Task AddPaintingReportAsync(Guid paintingUid, Guid reporter, string reason, DateTime now, int? roundId, int? serverId);
    Task<List<PaintingReportRecord>> GetPaintingReportsAsync(bool resolved, int limit);
    Task<PaintingReportRecord?> GetPaintingReportAsync(int reportId);
    Task<bool> ResolvePaintingReportAsync(int reportId, Guid resolver, DateTime now);
    Task<int> ResolvePaintingReportsForAsync(Guid paintingUid, Guid resolver, DateTime now);
}

public sealed partial class ServerDbManager
{
    public Task<bool> HasOpenPaintingReportAsync(Guid paintingUid, Guid reporter)
    {
        DbReadOpsMetric.Inc();
        return RunDbCommand(() => _db.HasOpenPaintingReport(paintingUid, reporter));
    }

    public Task AddPaintingReportAsync(Guid paintingUid, Guid reporter, string reason, DateTime now, int? roundId, int? serverId)
    {
        DbWriteOpsMetric.Inc();
        return RunDbCommand(() => _db.AddPaintingReport(paintingUid, reporter, reason, now, roundId, serverId));
    }

    public Task<List<PaintingReportRecord>> GetPaintingReportsAsync(bool resolved, int limit)
    {
        DbReadOpsMetric.Inc();
        return RunDbCommand(() => _db.GetPaintingReports(resolved, limit));
    }

    public Task<PaintingReportRecord?> GetPaintingReportAsync(int reportId)
    {
        DbReadOpsMetric.Inc();
        return RunDbCommand(() => _db.GetPaintingReport(reportId));
    }

    public Task<bool> ResolvePaintingReportAsync(int reportId, Guid resolver, DateTime now)
    {
        DbWriteOpsMetric.Inc();
        return RunDbCommand(() => _db.ResolvePaintingReport(reportId, resolver, now));
    }

    public Task<int> ResolvePaintingReportsForAsync(Guid paintingUid, Guid resolver, DateTime now)
    {
        DbWriteOpsMetric.Inc();
        return RunDbCommand(() => _db.ResolvePaintingReportsFor(paintingUid, resolver, now));
    }
}
