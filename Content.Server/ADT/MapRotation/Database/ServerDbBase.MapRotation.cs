using System.Linq;
using System.Threading.Tasks;
using Content.Server.ADT.MapRotation;
using Microsoft.EntityFrameworkCore;

namespace Content.Server.Database;

public abstract partial class ServerDbBase
{
    public async Task<List<MapRotationRecord>> GetMapRotation(string server)
    {
        await using var db = await GetDb();

        var rows = await db.DbContext.AdtMapRotation
            .AsNoTracking()
            .Where(r => r.Server == server)
            .ToListAsync();

        return rows
            .Select(r => new MapRotationRecord(r.MapId, r.BonusPerPlayer, SpecifyUtc(r.LastPlayed)))
            .ToList();
    }

    public async Task SaveMapRotation(string server, List<MapRotationRecord> records)
    {
        await using var db = await GetDb();

        var rows = await db.DbContext.AdtMapRotation
            .Where(r => r.Server == server)
            .ToListAsync();

        var byMap = rows.ToDictionary(r => r.MapId);
        var keep = new HashSet<string>();

        foreach (var record in records)
        {
            keep.Add(record.MapId);

            if (!byMap.TryGetValue(record.MapId, out var row))
            {
                row = new AdtMapRotation
                {
                    Server = server,
                    MapId = record.MapId,
                };

                db.DbContext.AdtMapRotation.Add(row);
            }

            row.BonusPerPlayer = record.BonusPerPlayer;
            row.LastPlayed = record.LastPlayed;
        }

        foreach (var row in rows)
        {
            if (!keep.Contains(row.MapId))
                db.DbContext.AdtMapRotation.Remove(row);
        }

        await db.DbContext.SaveChangesAsync();
    }
}
