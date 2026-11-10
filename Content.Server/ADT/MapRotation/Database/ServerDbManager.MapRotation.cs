using System.Threading.Tasks;
using Content.Server.ADT.MapRotation;

namespace Content.Server.Database;

public partial interface IServerDbManager
{
    Task<List<MapRotationRecord>> GetMapRotationAsync(string server);
    Task SaveMapRotationAsync(string server, List<MapRotationRecord> records);
}

public sealed partial class ServerDbManager
{
    public Task<List<MapRotationRecord>> GetMapRotationAsync(string server)
    {
        DbReadOpsMetric.Inc();
        return RunDbCommand(() => _db.GetMapRotation(server));
    }

    public Task SaveMapRotationAsync(string server, List<MapRotationRecord> records)
    {
        DbWriteOpsMetric.Inc();
        return RunDbCommand(() => _db.SaveMapRotation(server, records));
    }
}
