using System.Threading;
using System.Threading.Tasks;
using Content.Server.ADT.Administration.Logs;

namespace Content.Server.Database;

public partial interface IServerDbManager
{
    Task<bool> AddAdminLogChunks(List<AdtAdminLogChunk> chunks);
    Task<List<AdtLogChunkHeader>> GetAdminLogChunkHeaders(int round, CancellationToken cancel = default);
    Task<Dictionary<int, byte[]>> GetAdminLogChunkPayloads(int round, int[] indices, CancellationToken cancel = default);
    Task<int> GetLastAdminLogChunkIndex(int round);
}

public sealed partial class ServerDbManager
{
    public Task<bool> AddAdminLogChunks(List<AdtAdminLogChunk> chunks)
    {
        DbWriteOpsMetric.Inc();
        return RunDbCommand(() => _db.AddAdminLogChunks(chunks));
    }

    public Task<List<AdtLogChunkHeader>> GetAdminLogChunkHeaders(int round, CancellationToken cancel = default)
    {
        DbReadOpsMetric.Inc();
        return RunDbCommand(() => _db.GetAdminLogChunkHeaders(round, cancel));
    }

    public Task<Dictionary<int, byte[]>> GetAdminLogChunkPayloads(
        int round,
        int[] indices,
        CancellationToken cancel = default)
    {
        DbReadOpsMetric.Inc();
        return RunDbCommand(() => _db.GetAdminLogChunkPayloads(round, indices, cancel));
    }

    public Task<int> GetLastAdminLogChunkIndex(int round)
    {
        DbReadOpsMetric.Inc();
        return RunDbCommand(() => _db.GetLastAdminLogChunkIndex(round));
    }
}
