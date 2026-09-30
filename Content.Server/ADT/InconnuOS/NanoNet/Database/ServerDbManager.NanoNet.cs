using System.Threading.Tasks;
using Content.Server.ADT.InconnuOS.NanoNet;

namespace Content.Server.Database;

public partial interface IServerDbManager
{
    Task<List<NanoNetStoredSite>> GetNanoNetSitesAsync();
    Task SyncNanoNetSitesAsync(IReadOnlyCollection<NanoNetStoredSite> upsert, IReadOnlyCollection<string> delete);
}

public sealed partial class ServerDbManager
{
    public Task<List<NanoNetStoredSite>> GetNanoNetSitesAsync()
    {
        DbReadOpsMetric.Inc();
        return RunDbCommand(() => _db.GetNanoNetSites());
    }

    public Task SyncNanoNetSitesAsync(IReadOnlyCollection<NanoNetStoredSite> upsert, IReadOnlyCollection<string> delete)
    {
        DbWriteOpsMetric.Inc();
        return RunDbCommand(() => _db.SyncNanoNetSites(upsert, delete));
    }
}
