using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Content.Server.ADT.Administration.Logs;
using Microsoft.EntityFrameworkCore;

namespace Content.Server.Database;

public abstract partial class ServerDbBase
{
    public async Task<bool> AddAdminLogChunks(List<AdtAdminLogChunk> chunks)
    {
        const int maxRetryAttempts = 5;

        var retryDelay = TimeSpan.FromSeconds(5);
        var attempt = 0;

        while (attempt < maxRetryAttempts)
        {
            try
            {
                await using var db = await GetDb();
                db.DbContext.AdtAdminLogChunk.AddRange(chunks);
                await db.DbContext.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                attempt += 1;
                _opsLog.Error($"Attempt {attempt} failed to save admin log chunks: {ex}");

                if (attempt >= maxRetryAttempts)
                {
                    _opsLog.Error($"Max retry attempts reached. Failed to save {chunks.Count} admin log chunks.");
                    return false;
                }

                await Task.Delay(retryDelay);
                retryDelay *= 2;
            }
        }

        return false;
    }

    public async Task<List<AdtLogChunkHeader>> GetAdminLogChunkHeaders(int round, CancellationToken cancel = default)
    {
        await using var db = await GetDb(cancel);

        var rows = await db.DbContext.AdtAdminLogChunk
            .AsNoTracking()
            .Where(chunk => chunk.RoundId == round)
            .OrderBy(chunk => chunk.ChunkIndex)
            .Select(chunk => new
            {
                chunk.RoundId,
                chunk.ChunkIndex,
                chunk.Format,
                chunk.LogCount,
                chunk.FirstLogId,
                chunk.LastLogId,
                chunk.FirstDate,
                chunk.LastDate,
                chunk.RawSize,
                chunk.Flags,
                chunk.ImpactMask,
                chunk.TypeMask,
                chunk.Players,
            })
            .ToListAsync(cancel);

        var headers = new List<AdtLogChunkHeader>(rows.Count);

        foreach (var row in rows)
        {
            headers.Add(new AdtLogChunkHeader
            {
                RoundId = row.RoundId,
                ChunkIndex = row.ChunkIndex,
                Format = row.Format,
                LogCount = row.LogCount,
                FirstLogId = row.FirstLogId,
                LastLogId = row.LastLogId,
                FirstDate = row.FirstDate,
                LastDate = row.LastDate,
                RawSize = row.RawSize,
                Flags = row.Flags,
                ImpactMask = row.ImpactMask,
                TypeMask = row.TypeMask,
                Players = AdtLogChunkFormat.ReadPlayers(row.Players),
            });
        }

        return headers;
    }

    public async Task<Dictionary<int, byte[]>> GetAdminLogChunkPayloads(
        int round,
        int[] indices,
        CancellationToken cancel = default)
    {
        await using var db = await GetDb(cancel);

        var rows = await db.DbContext.AdtAdminLogChunk
            .AsNoTracking()
            .Where(chunk => chunk.RoundId == round && indices.Contains(chunk.ChunkIndex))
            .Select(chunk => new { chunk.ChunkIndex, chunk.Payload })
            .ToListAsync(cancel);

        var payloads = new Dictionary<int, byte[]>(rows.Count);

        foreach (var row in rows)
        {
            payloads[row.ChunkIndex] = row.Payload;
        }

        return payloads;
    }

    public async Task<int> GetLastAdminLogChunkIndex(int round)
    {
        await using var db = await GetDb();

        var last = await db.DbContext.AdtAdminLogChunk
            .AsNoTracking()
            .Where(chunk => chunk.RoundId == round)
            .MaxAsync(chunk => (int?) chunk.ChunkIndex);

        return last ?? -1;
    }
}
