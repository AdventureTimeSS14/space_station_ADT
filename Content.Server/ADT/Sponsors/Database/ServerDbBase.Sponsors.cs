using System.Linq;
using System.Threading.Tasks;
using Content.Server.ADT.Sponsors;
using Content.Shared.ADT.Sponsors;
using Microsoft.EntityFrameworkCore;

namespace Content.Server.Database;

public abstract partial class ServerDbBase
{
    #region Тиры

    public async Task<List<SponsorTier>> GetAllSponsorTiers()
    {
        await using var db = await GetDb();

        var rows = await db.DbContext.AdtSponsorTier
            .AsNoTracking()
            .OrderBy(t => t.Id)
            .ToListAsync();

        var result = new List<SponsorTier>(rows.Count);

        foreach (var row in rows)
        {
            result.Add(ToShared(row));
        }

        return result;
    }

    public async Task<SponsorTier?> GetSponsorTier(int tierId)
    {
        await using var db = await GetDb();

        var row = await db.DbContext.AdtSponsorTier
            .AsNoTracking()
            .SingleOrDefaultAsync(t => t.Id == tierId);

        if (row == null)
            return null;

        return ToShared(row);
    }

    public async Task<SponsorTier?> GetSponsorTierByName(string name)
    {
        await using var db = await GetDb();

        var row = await db.DbContext.AdtSponsorTier
            .AsNoTracking()
            .SingleOrDefaultAsync(t => t.Name == name);

        if (row == null)
            return null;

        return ToShared(row);
    }

    public async Task<SponsorTier> CreateSponsorTier(SponsorTier tier)
    {
        await using var db = await GetDb();

        var row = new AdtSponsorTier
        {
            Name = tier.Name,
            DisplayName = tier.DisplayName,
            Description = tier.Description,
            Priority = tier.Priority,
            Enabled = tier.Enabled,
            Benefits = SponsorSerialization.SerializeBenefits(tier.Benefits),
            CreatedAt = DateTime.UtcNow,
        };

        db.DbContext.AdtSponsorTier.Add(row);
        await db.DbContext.SaveChangesAsync();

        return ToShared(row);
    }

    public async Task<bool> UpdateSponsorTier(SponsorTier tier)
    {
        await using var db = await GetDb();

        var row = await db.DbContext.AdtSponsorTier
            .SingleOrDefaultAsync(t => t.Id == tier.Id);

        if (row == null)
            return false;

        row.Name = tier.Name;
        row.DisplayName = tier.DisplayName;
        row.Description = tier.Description;
        row.Priority = tier.Priority;
        row.Enabled = tier.Enabled;
        row.Benefits = SponsorSerialization.SerializeBenefits(tier.Benefits);

        await db.DbContext.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteSponsorTier(int tierId)
    {
        await using var db = await GetDb();

        var row = await db.DbContext.AdtSponsorTier
            .SingleOrDefaultAsync(t => t.Id == tierId);

        if (row == null)
            return false;

        db.DbContext.AdtSponsorTier.Remove(row);
        await db.DbContext.SaveChangesAsync();
        return true;
    }

    public async Task<bool> SponsorTierNameTaken(string name, int exceptId)
    {
        await using var db = await GetDb();

        return await db.DbContext.AdtSponsorTier
            .AnyAsync(t => t.Name == name && t.Id != exceptId);
    }

    #endregion

    #region Выдачи

    public async Task<List<SponsorGrant>> GetSponsorGrants(Guid userId)
    {
        await using var db = await GetDb();

        var rows = await db.DbContext.AdtSponsorGrant
            .AsNoTracking()
            .Include(g => g.Tier)
            .Where(g => g.UserId == userId && !g.Revoked)
            .ToListAsync();

        return ToShared(rows);
    }

    public async Task<List<SponsorGrant>> GetAllSponsorGrants()
    {
        await using var db = await GetDb();

        var rows = await db.DbContext.AdtSponsorGrant
            .AsNoTracking()
            .Include(g => g.Tier)
            .Where(g => !g.Revoked)
            .ToListAsync();

        return ToShared(rows);
    }

    public async Task<List<SponsorGrant>> GetSponsorGrantHistory(Guid userId)
    {
        await using var db = await GetDb();

        var rows = await db.DbContext.AdtSponsorGrant
            .AsNoTracking()
            .Include(g => g.Tier)
            .Where(g => g.UserId == userId)
            .OrderByDescending(g => g.CreatedAt)
            .ToListAsync();

        return ToShared(rows);
    }

    public async Task<SponsorGrant?> GetSponsorGrant(int grantId)
    {
        await using var db = await GetDb();

        var row = await db.DbContext.AdtSponsorGrant
            .AsNoTracking()
            .Include(g => g.Tier)
            .SingleOrDefaultAsync(g => g.Id == grantId);

        if (row == null)
            return null;

        return ToShared(row);
    }

    public async Task<SponsorGrant> CreateSponsorGrant(SponsorGrant grant)
    {
        await using var db = await GetDb();

        var row = new AdtSponsorGrant
        {
            UserId = grant.UserId,
            TierId = grant.TierId,
            Priority = grant.Priority,
            Overrides = grant.Overrides == null ? null : SponsorSerialization.SerializeBenefits(grant.Overrides),
            Comment = grant.Comment,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = grant.CreatedBy,
            ExpiresAt = ToUtc(grant.ExpiresAt),
            Revoked = false,
        };

        db.DbContext.AdtSponsorGrant.Add(row);
        await db.DbContext.SaveChangesAsync();

        await db.DbContext.Entry(row).Reference(g => g.Tier).LoadAsync();

        return ToShared(row);
    }

    public async Task<bool> UpdateSponsorGrant(SponsorGrant grant)
    {
        await using var db = await GetDb();

        var row = await db.DbContext.AdtSponsorGrant
            .SingleOrDefaultAsync(g => g.Id == grant.Id);

        if (row == null)
            return false;

        row.TierId = grant.TierId;
        row.Priority = grant.Priority;
        row.Overrides = grant.Overrides == null ? null : SponsorSerialization.SerializeBenefits(grant.Overrides);
        row.Comment = grant.Comment;
        row.ExpiresAt = ToUtc(grant.ExpiresAt);

        await db.DbContext.SaveChangesAsync();
        return true;
    }

    public async Task<bool> RevokeSponsorGrant(int grantId, Guid? revokedBy)
    {
        await using var db = await GetDb();

        var row = await db.DbContext.AdtSponsorGrant
            .SingleOrDefaultAsync(g => g.Id == grantId);

        if (row == null || row.Revoked)
            return false;

        row.Revoked = true;
        row.RevokedAt = DateTime.UtcNow;
        row.RevokedBy = revokedBy;

        await db.DbContext.SaveChangesAsync();
        return true;
    }

    #endregion

    #region Личные настройки

    public async Task<SponsorPersonalColors?> GetSponsorColors(Guid userId)
    {
        await using var db = await GetDb();

        var row = await db.DbContext.AdtSponsorPreference
            .AsNoTracking()
            .SingleOrDefaultAsync(p => p.UserId == userId);

        if (row == null)
            return null;

        return new SponsorPersonalColors
        {
            Ooc = ParseColor(row.OocColor),
            Ghost = ParseColor(row.GhostColor),
        };
    }

    public async Task SaveSponsorColors(Guid userId, SponsorPersonalColors colors)
    {
        try
        {
            await SaveSponsorColorsCore(userId, colors);
        }
        catch (DbUpdateException)
        {
            await SaveSponsorColorsCore(userId, colors);
        }
    }

    private async Task SaveSponsorColorsCore(Guid userId, SponsorPersonalColors colors)
    {
        await using var db = await GetDb();

        var row = await db.DbContext.AdtSponsorPreference
            .SingleOrDefaultAsync(p => p.UserId == userId);

        if (row == null)
        {
            row = new AdtSponsorPreference
            {
                UserId = userId,
            };

            db.DbContext.AdtSponsorPreference.Add(row);
        }

        row.OocColor = colors.Ooc?.ToHex();
        row.GhostColor = colors.Ghost?.ToHex();

        await db.DbContext.SaveChangesAsync();
    }

    private static Color? ParseColor(string? hex)
    {
        if (string.IsNullOrWhiteSpace(hex))
            return null;

        return Color.TryFromHex(hex);
    }

    #endregion

    #region Маппинг

    private static SponsorTier ToShared(AdtSponsorTier row)
    {
        SponsorSerialization.TryDeserializeBenefits(row.Benefits, out var benefits);

        return new SponsorTier
        {
            Id = row.Id,
            Name = row.Name,
            DisplayName = row.DisplayName,
            Description = row.Description,
            Priority = row.Priority,
            Enabled = row.Enabled,
            Benefits = benefits,
            CreatedAt = SpecifyUtc(row.CreatedAt),
        };
    }

    private static List<SponsorGrant> ToShared(List<AdtSponsorGrant> rows)
    {
        var result = new List<SponsorGrant>(rows.Count);

        foreach (var row in rows)
        {
            result.Add(ToShared(row));
        }

        return result;
    }

    private static SponsorGrant ToShared(AdtSponsorGrant row)
    {
        SponsorBenefits? overrides = null;

        if (row.Overrides != null)
        {
            SponsorSerialization.TryDeserializeBenefits(row.Overrides, out var parsed);
            overrides = parsed;
        }

        return new SponsorGrant
        {
            Id = row.Id,
            UserId = row.UserId,
            TierId = row.TierId,
            TierName = row.Tier?.Name,
            Priority = row.Priority,
            Overrides = overrides,
            Comment = row.Comment,
            CreatedAt = SpecifyUtc(row.CreatedAt),
            CreatedBy = row.CreatedBy,
            ExpiresAt = SpecifyUtc(row.ExpiresAt),
            Revoked = row.Revoked,
            RevokedAt = SpecifyUtc(row.RevokedAt),
            RevokedBy = row.RevokedBy,
        };
    }

    private static DateTime? ToUtc(DateTime? value)
    {
        if (value == null)
            return null;

        return value.Value.Kind switch
        {
            DateTimeKind.Utc => value.Value,
            DateTimeKind.Local => value.Value.ToUniversalTime(),
            _ => DateTime.SpecifyKind(value.Value, DateTimeKind.Utc),
        };
    }

    private static DateTime SpecifyUtc(DateTime value)
    {
        if (value.Kind == DateTimeKind.Utc)
            return value;

        return DateTime.SpecifyKind(value, DateTimeKind.Utc);
    }

    private static DateTime? SpecifyUtc(DateTime? value)
    {
        if (value == null)
            return null;

        return SpecifyUtc(value.Value);
    }

    #endregion
}
