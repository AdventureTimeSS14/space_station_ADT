using System.Linq;
using System.Threading.Tasks;
using Content.Server.ADT.Polls;
using Content.Shared.ADT.Polls;
using Microsoft.EntityFrameworkCore;

namespace Content.Server.Database;

public abstract partial class ServerDbBase
{
    public async Task<List<PollData>> GetActivePolls()
    {
        await using var db = await GetDb();

        var now = DateTime.UtcNow;

        var rows = await db.DbContext.AdtPoll
            .AsNoTracking()
            .Where(p => !p.Closed && (p.EndsAt == null || p.EndsAt > now))
            .OrderBy(p => p.Id)
            .ToListAsync();

        return rows.Select(ToShared).ToList();
    }

    public async Task<List<PollAdminEntry>> GetPollAdminEntries()
    {
        await using var db = await GetDb();

        var rows = await db.DbContext.AdtPoll
            .AsNoTracking()
            .OrderByDescending(p => p.Id)
            .Select(p => new
            {
                Poll = p,
                Responses = p.Responses.Count,
                Pending = p.Pending.Count,
            })
            .ToListAsync();

        var result = new List<PollAdminEntry>(rows.Count);

        foreach (var row in rows)
        {
            result.Add(new PollAdminEntry
            {
                Poll = ToShared(row.Poll),
                Responses = row.Responses,
                Pending = row.Pending,
            });
        }

        return result;
    }

    public async Task<PollData?> GetPoll(int pollId)
    {
        await using var db = await GetDb();

        var row = await db.DbContext.AdtPoll
            .AsNoTracking()
            .SingleOrDefaultAsync(p => p.Id == pollId);

        if (row == null)
            return null;

        return ToShared(row);
    }

    public async Task<int> GetPollResponseCount(int pollId)
    {
        await using var db = await GetDb();

        return await db.DbContext.AdtPollResponse
            .CountAsync(r => r.PollId == pollId);
    }

    public async Task<PollData> CreatePoll(PollData poll)
    {
        await using var db = await GetDb();

        var row = new AdtPoll
        {
            CreatedBy = poll.CreatedBy,
            CreatedAt = DateTime.UtcNow,
        };

        Fill(row, poll);

        db.DbContext.AdtPoll.Add(row);
        await db.DbContext.SaveChangesAsync();

        return ToShared(row);
    }

    public async Task<bool> UpdatePoll(PollData poll)
    {
        await using var db = await GetDb();

        var row = await db.DbContext.AdtPoll
            .SingleOrDefaultAsync(p => p.Id == poll.Id);

        if (row == null)
            return false;

        Fill(row, poll);

        await db.DbContext.SaveChangesAsync();
        return true;
    }

    public async Task<bool> SetPollClosed(int pollId, bool closed)
    {
        await using var db = await GetDb();

        var row = await db.DbContext.AdtPoll
            .SingleOrDefaultAsync(p => p.Id == pollId);

        if (row == null)
            return false;

        row.Closed = closed;

        await db.DbContext.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeletePoll(int pollId)
    {
        await using var db = await GetDb();

        await db.DbContext.AdtPollResponse
            .Where(r => r.PollId == pollId)
            .ExecuteDeleteAsync();

        await db.DbContext.AdtPollPending
            .Where(r => r.PollId == pollId)
            .ExecuteDeleteAsync();

        var deleted = await db.DbContext.AdtPoll
            .Where(p => p.Id == pollId)
            .ExecuteDeleteAsync();

        return deleted > 0;
    }

    public async Task<int> WipePollResponses(int pollId)
    {
        await using var db = await GetDb();

        return await db.DbContext.AdtPollResponse
            .Where(r => r.PollId == pollId)
            .ExecuteDeleteAsync();
    }

    public async Task<PollUserState> GetPollUserState(Guid userId)
    {
        await using var db = await GetDb();

        var answered = await db.DbContext.AdtPollResponse
            .Where(r => r.UserId == userId)
            .Select(r => r.PollId)
            .ToListAsync();

        var pending = await db.DbContext.AdtPollPending
            .AsNoTracking()
            .Where(r => r.UserId == userId)
            .ToListAsync();

        var state = new PollUserState();
        state.Answered.UnionWith(answered);

        foreach (var row in pending)
        {
            state.Pending[row.PollId] = PollSerialization.Deserialize<PollSnapshot>(row.Snapshot);
        }

        return state;
    }

    public async Task AddPollPending(List<PollPendingEntry> entries)
    {
        if (entries.Count == 0)
            return;

        await using var db = await GetDb();

        var pollIds = entries.Select(e => e.PollId).Distinct().ToList();
        var userIds = entries.Select(e => e.UserId).Distinct().ToList();

        var existing = await db.DbContext.AdtPollPending
            .Where(r => pollIds.Contains(r.PollId) && userIds.Contains(r.UserId))
            .Select(r => new { r.PollId, r.UserId })
            .ToListAsync();

        var answered = await db.DbContext.AdtPollResponse
            .Where(r => pollIds.Contains(r.PollId) && userIds.Contains(r.UserId))
            .Select(r => new { r.PollId, r.UserId })
            .ToListAsync();

        var skip = new HashSet<(int, Guid)>();

        foreach (var row in existing)
        {
            skip.Add((row.PollId, row.UserId));
        }

        foreach (var row in answered)
        {
            skip.Add((row.PollId, row.UserId));
        }

        var now = DateTime.UtcNow;

        foreach (var entry in entries)
        {
            if (!skip.Add((entry.PollId, entry.UserId)))
                continue;

            db.DbContext.AdtPollPending.Add(new AdtPollPending
            {
                PollId = entry.PollId,
                UserId = entry.UserId,
                Snapshot = PollSerialization.Serialize(entry.Snapshot),
                CreatedAt = now,
            });
        }

        await db.DbContext.SaveChangesAsync();
    }

    public async Task<bool> SubmitPollResponse(PollResponseView response, int pollId)
    {
        await using var db = await GetDb();

        var exists = await db.DbContext.AdtPollResponse
            .AnyAsync(r => r.PollId == pollId && r.UserId == response.UserId);

        if (exists)
            return false;

        db.DbContext.AdtPollResponse.Add(new AdtPollResponse
        {
            PollId = pollId,
            UserId = response.UserId,
            RoundId = response.RoundId,
            Server = response.Server,
            SubmittedAt = DateTime.UtcNow,
            Targeted = response.Targeted,
            Snapshot = PollSerialization.Serialize(response.Snapshot),
            Answers = PollSerialization.Serialize(response.Answers),
        });

        try
        {
            await db.DbContext.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            return false;
        }

        await db.DbContext.AdtPollPending
            .Where(r => r.PollId == pollId && r.UserId == response.UserId)
            .ExecuteDeleteAsync();

        return true;
    }

    public async Task<List<PollResponseView>> GetPollResponses(int pollId)
    {
        await using var db = await GetDb();

        var rows = await (
                from r in db.DbContext.AdtPollResponse.AsNoTracking()
                where r.PollId == pollId
                join p in db.DbContext.Player on r.UserId equals p.UserId into players
                from p in players.DefaultIfEmpty()
                orderby r.Id
                select new
                {
                    Response = r,
                    UserName = p == null ? null : p.LastSeenUserName,
                })
            .ToListAsync();

        var result = new List<PollResponseView>(rows.Count);

        foreach (var row in rows)
        {
            result.Add(new PollResponseView
            {
                UserId = row.Response.UserId,
                UserName = row.UserName ?? row.Response.UserId.ToString(),
                SubmittedAt = DateTime.SpecifyKind(row.Response.SubmittedAt, DateTimeKind.Utc),
                RoundId = row.Response.RoundId,
                Server = row.Response.Server,
                Targeted = row.Response.Targeted,
                Snapshot = PollSerialization.Deserialize<PollSnapshot>(row.Response.Snapshot),
                Answers = PollSerialization.Deserialize<List<PollAnswer>>(row.Response.Answers),
            });
        }

        return result;
    }

    public async Task<int> CleanupPolls(DateTime cutoff)
    {
        await using var db = await GetDb();

        var now = DateTime.UtcNow;

        var inactive = await db.DbContext.AdtPoll
            .Where(p => p.Closed || (p.EndsAt != null && p.EndsAt < now))
            .Select(p => new { p.Id, Stamp = p.EndsAt ?? p.CreatedAt })
            .ToListAsync();

        if (inactive.Count == 0)
            return 0;

        var inactiveIds = inactive.Select(p => p.Id).ToList();

        await db.DbContext.AdtPollPending
            .Where(r => inactiveIds.Contains(r.PollId))
            .ExecuteDeleteAsync();

        var old = inactive
            .Where(p => p.Stamp < cutoff)
            .Select(p => p.Id)
            .ToList();

        if (old.Count == 0)
            return 0;

        await db.DbContext.AdtPollResponse
            .Where(r => old.Contains(r.PollId))
            .ExecuteDeleteAsync();

        await db.DbContext.AdtPollPending
            .Where(r => old.Contains(r.PollId))
            .ExecuteDeleteAsync();

        return await db.DbContext.AdtPoll
            .Where(p => old.Contains(p.Id))
            .ExecuteDeleteAsync();
    }

    private static void Fill(AdtPoll row, PollData poll)
    {
        row.Title = poll.Title;
        row.Description = poll.Description;
        row.Questions = PollSerialization.Serialize(poll.Questions);
        row.Filters = PollSerialization.Serialize(poll.Filters);
        row.Servers = PollSerialization.Serialize(poll.Servers);
        row.ShowMode = (int) poll.ShowMode;
        row.EndsAt = poll.EndsAt;
        row.Closed = poll.Closed;
    }

    private static PollData ToShared(AdtPoll row)
    {
        return new PollData
        {
            Id = row.Id,
            Title = row.Title,
            Description = row.Description,
            Questions = PollSerialization.Deserialize<List<PollQuestion>>(row.Questions),
            Filters = PollSerialization.Deserialize<PollFilters>(row.Filters),
            Servers = PollSerialization.Deserialize<HashSet<string>>(row.Servers),
            ShowMode = (PollShowMode) row.ShowMode,
            CreatedBy = row.CreatedBy,
            CreatedAt = DateTime.SpecifyKind(row.CreatedAt, DateTimeKind.Utc),
            EndsAt = row.EndsAt == null ? null : DateTime.SpecifyKind(row.EndsAt.Value, DateTimeKind.Utc),
            Closed = row.Closed,
        };
    }
}
