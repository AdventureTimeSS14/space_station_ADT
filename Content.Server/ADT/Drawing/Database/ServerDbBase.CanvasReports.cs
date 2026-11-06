using System.Linq;
using System.Threading.Tasks;
using Content.Server.ADT.Drawing.Database;
using Microsoft.EntityFrameworkCore;

namespace Content.Server.Database;

public abstract partial class ServerDbBase
{
    public async Task<bool> HasOpenPaintingReport(Guid paintingUid, Guid reporter)
    {
        await using var db = await GetDb();

        return await db.DbContext.Set<AdtPaintingReport>()
            .AnyAsync(r => r.PaintingUid == paintingUid && r.ReporterUserId == reporter && !r.Resolved);
    }

    public async Task AddPaintingReport(Guid paintingUid, Guid reporter, string reason, DateTime now, int? roundId, int? serverId)
    {
        await using var db = await GetDb();

        db.DbContext.Set<AdtPaintingReport>().Add(new AdtPaintingReport
        {
            PaintingUid = paintingUid,
            ReporterUserId = reporter,
            Reason = reason,
            CreatedAt = now,
            RoundId = roundId,
            ServerId = serverId,
        });

        await db.DbContext.SaveChangesAsync();
    }

    public async Task<List<PaintingReportRecord>> GetPaintingReports(bool resolved, int limit)
    {
        await using var db = await GetDb();

        var query = db.DbContext.Set<AdtPaintingReport>()
            .AsNoTracking()
            .Where(r => r.Resolved == resolved);

        query = resolved
            ? query.OrderByDescending(r => r.ResolvedAt)
            : query.OrderByDescending(r => r.CreatedAt);

        var rows = await query.Take(limit).ToListAsync();
        var uids = rows.Select(r => r.PaintingUid).Distinct().ToList();

        var paintings = await db.DbContext.Set<AdtPainting>()
            .AsNoTracking()
            .Where(p => uids.Contains(p.PaintingUid))
            .Select(p => new { p.PaintingUid, p.Title, p.Status, p.OwnerUserId })
            .ToDictionaryAsync(p => p.PaintingUid);

        var open = await db.DbContext.Set<AdtPaintingReport>()
            .Where(r => !r.Resolved && uids.Contains(r.PaintingUid))
            .GroupBy(r => r.PaintingUid)
            .Select(g => new { g.Key, Count = g.Count() })
            .ToDictionaryAsync(g => g.Key, g => g.Count);

        var result = new List<PaintingReportRecord>(rows.Count);
        foreach (var row in rows)
        {
            var record = ToReportRecord(row);
            if (paintings.TryGetValue(row.PaintingUid, out var painting))
            {
                record.Title = painting.Title;
                record.Status = painting.Status;
                record.OwnerUserId = painting.OwnerUserId;
            }

            record.OpenReports = open.GetValueOrDefault(row.PaintingUid);
            result.Add(record);
        }

        return result;
    }

    public async Task<PaintingReportRecord?> GetPaintingReport(int reportId)
    {
        await using var db = await GetDb();

        var row = await db.DbContext.Set<AdtPaintingReport>()
            .AsNoTracking()
            .SingleOrDefaultAsync(r => r.ReportId == reportId);

        if (row == null)
            return null;

        var record = ToReportRecord(row);
        var painting = await db.DbContext.Set<AdtPainting>()
            .AsNoTracking()
            .Where(p => p.PaintingUid == row.PaintingUid)
            .Select(p => new { p.Title, p.Status, p.OwnerUserId })
            .SingleOrDefaultAsync();

        if (painting != null)
        {
            record.Title = painting.Title;
            record.Status = painting.Status;
            record.OwnerUserId = painting.OwnerUserId;
        }

        return record;
    }

    public async Task<bool> ResolvePaintingReport(int reportId, Guid resolver, DateTime now)
    {
        await using var db = await GetDb();

        var updated = await db.DbContext.Set<AdtPaintingReport>()
            .Where(r => r.ReportId == reportId && !r.Resolved)
            .ExecuteUpdateAsync(s => s
                .SetProperty(r => r.Resolved, true)
                .SetProperty(r => r.ResolvedBy, resolver)
                .SetProperty(r => r.ResolvedAt, now));

        return updated > 0;
    }

    public async Task<int> ResolvePaintingReportsFor(Guid paintingUid, Guid resolver, DateTime now)
    {
        await using var db = await GetDb();

        return await db.DbContext.Set<AdtPaintingReport>()
            .Where(r => r.PaintingUid == paintingUid && !r.Resolved)
            .ExecuteUpdateAsync(s => s
                .SetProperty(r => r.Resolved, true)
                .SetProperty(r => r.ResolvedBy, resolver)
                .SetProperty(r => r.ResolvedAt, now));
    }

    private static PaintingReportRecord ToReportRecord(AdtPaintingReport row)
    {
        return new PaintingReportRecord
        {
            ReportId = row.ReportId,
            PaintingUid = row.PaintingUid,
            ReporterUserId = row.ReporterUserId,
            Reason = row.Reason,
            CreatedAt = row.CreatedAt,
            RoundId = row.RoundId,
            Resolved = row.Resolved,
            ResolvedBy = row.ResolvedBy,
            ResolvedAt = row.ResolvedAt,
        };
    }
}
