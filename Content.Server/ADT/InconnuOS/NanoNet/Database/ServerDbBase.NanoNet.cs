using System.Linq;
using System.Threading.Tasks;
using Content.Server.ADT.InconnuOS.NanoNet;
using Microsoft.EntityFrameworkCore;

namespace Content.Server.Database;

public abstract partial class ServerDbBase
{
    public async Task<List<NanoNetStoredSite>> GetNanoNetSites()
    {
        await using var db = await GetDb();

        return await db.DbContext.AdtNanoNetSite
            .AsNoTracking()
            .Select(s => new NanoNetStoredSite(s.Label, s.UserId, s.OwnerName, s.Html, s.PublishedAt))
            .ToListAsync();
    }

    public async Task SyncNanoNetSites(IReadOnlyCollection<NanoNetStoredSite> upsert, IReadOnlyCollection<string> delete)
    {
        await using var db = await GetDb();

        var labels = upsert.Select(s => s.Label).Concat(delete).ToList();

        var rows = await db.DbContext.AdtNanoNetSite
            .Where(s => labels.Contains(s.Label))
            .ToDictionaryAsync(s => s.Label);

        foreach (var label in delete)
        {
            if (rows.Remove(label, out var row))
                db.DbContext.AdtNanoNetSite.Remove(row);
        }

        foreach (var site in upsert)
        {
            if (!rows.TryGetValue(site.Label, out var row))
            {
                row = new AdtNanoNetSite { Label = site.Label };
                db.DbContext.AdtNanoNetSite.Add(row);
            }

            row.UserId = site.UserId;
            row.OwnerName = site.OwnerName;
            row.Html = site.Html;
            row.PublishedAt = site.PublishedAt;
        }

        await db.DbContext.SaveChangesAsync();
    }
}
