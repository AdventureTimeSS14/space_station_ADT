using System.Threading.Tasks;
using Content.Shared.ADT.Sponsors;

namespace Content.Server.Database;

public partial interface IServerDbManager
{
    #region Тиры

    Task<List<SponsorTier>> GetAllSponsorTiersAsync();
    Task<SponsorTier?> GetSponsorTierAsync(int tierId);
    Task<SponsorTier?> GetSponsorTierByNameAsync(string name);
    Task<SponsorTier> CreateSponsorTierAsync(SponsorTier tier);
    Task<bool> UpdateSponsorTierAsync(SponsorTier tier);
    Task<bool> DeleteSponsorTierAsync(int tierId);
    Task<bool> SponsorTierNameTakenAsync(string name, int exceptId);

    #endregion

    #region Выдачи

    Task<List<SponsorGrant>> GetSponsorGrantsAsync(Guid userId);
    Task<List<SponsorGrant>> GetAllSponsorGrantsAsync();
    Task<List<SponsorGrant>> GetSponsorGrantHistoryAsync(Guid userId);
    Task<SponsorGrant?> GetSponsorGrantAsync(int grantId);
    Task<SponsorGrant> CreateSponsorGrantAsync(SponsorGrant grant);
    Task<bool> UpdateSponsorGrantAsync(SponsorGrant grant);
    Task<bool> RevokeSponsorGrantAsync(int grantId, Guid? revokedBy);

    #endregion

    #region Личные настройки

    Task<SponsorPersonalColors?> GetSponsorColorsAsync(Guid userId);
    Task SaveSponsorColorsAsync(Guid userId, SponsorPersonalColors colors);

    #endregion
}

public sealed partial class ServerDbManager
{
    #region Тиры

    public Task<List<SponsorTier>> GetAllSponsorTiersAsync()
    {
        DbReadOpsMetric.Inc();
        return RunDbCommand(() => _db.GetAllSponsorTiers());
    }

    public Task<SponsorTier?> GetSponsorTierAsync(int tierId)
    {
        DbReadOpsMetric.Inc();
        return RunDbCommand(() => _db.GetSponsorTier(tierId));
    }

    public Task<SponsorTier?> GetSponsorTierByNameAsync(string name)
    {
        DbReadOpsMetric.Inc();
        return RunDbCommand(() => _db.GetSponsorTierByName(name));
    }

    public Task<SponsorTier> CreateSponsorTierAsync(SponsorTier tier)
    {
        DbWriteOpsMetric.Inc();
        return RunDbCommand(() => _db.CreateSponsorTier(tier));
    }

    public Task<bool> UpdateSponsorTierAsync(SponsorTier tier)
    {
        DbWriteOpsMetric.Inc();
        return RunDbCommand(() => _db.UpdateSponsorTier(tier));
    }

    public Task<bool> DeleteSponsorTierAsync(int tierId)
    {
        DbWriteOpsMetric.Inc();
        return RunDbCommand(() => _db.DeleteSponsorTier(tierId));
    }

    public Task<bool> SponsorTierNameTakenAsync(string name, int exceptId)
    {
        DbReadOpsMetric.Inc();
        return RunDbCommand(() => _db.SponsorTierNameTaken(name, exceptId));
    }

    #endregion

    #region Выдачи

    public Task<List<SponsorGrant>> GetSponsorGrantsAsync(Guid userId)
    {
        DbReadOpsMetric.Inc();
        return RunDbCommand(() => _db.GetSponsorGrants(userId));
    }

    public Task<List<SponsorGrant>> GetAllSponsorGrantsAsync()
    {
        DbReadOpsMetric.Inc();
        return RunDbCommand(() => _db.GetAllSponsorGrants());
    }

    public Task<List<SponsorGrant>> GetSponsorGrantHistoryAsync(Guid userId)
    {
        DbReadOpsMetric.Inc();
        return RunDbCommand(() => _db.GetSponsorGrantHistory(userId));
    }

    public Task<SponsorGrant?> GetSponsorGrantAsync(int grantId)
    {
        DbReadOpsMetric.Inc();
        return RunDbCommand(() => _db.GetSponsorGrant(grantId));
    }

    public Task<SponsorGrant> CreateSponsorGrantAsync(SponsorGrant grant)
    {
        DbWriteOpsMetric.Inc();
        return RunDbCommand(() => _db.CreateSponsorGrant(grant));
    }

    public Task<bool> UpdateSponsorGrantAsync(SponsorGrant grant)
    {
        DbWriteOpsMetric.Inc();
        return RunDbCommand(() => _db.UpdateSponsorGrant(grant));
    }

    public Task<bool> RevokeSponsorGrantAsync(int grantId, Guid? revokedBy)
    {
        DbWriteOpsMetric.Inc();
        return RunDbCommand(() => _db.RevokeSponsorGrant(grantId, revokedBy));
    }

    #endregion

    #region Личные настройки

    public Task<SponsorPersonalColors?> GetSponsorColorsAsync(Guid userId)
    {
        DbReadOpsMetric.Inc();
        return RunDbCommand(() => _db.GetSponsorColors(userId));
    }

    public Task SaveSponsorColorsAsync(Guid userId, SponsorPersonalColors colors)
    {
        DbWriteOpsMetric.Inc();
        return RunDbCommand(() => _db.SaveSponsorColors(userId, colors));
    }

    #endregion
}
