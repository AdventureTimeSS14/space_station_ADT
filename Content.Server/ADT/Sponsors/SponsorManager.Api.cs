using System.Linq;
using System.Threading.Tasks;
using Content.Server.Administration.Logs;
using Content.Shared.ADT.Sponsors;
using Content.Shared.Database;
using Microsoft.EntityFrameworkCore;
using Robust.Shared.Network;

namespace Content.Server.ADT.Sponsors;

public sealed partial class SponsorManager
{
    [Dependency] private readonly IAdminLogManager _adminLog = default!;

    public async Task<SponsorTier?> CreateTierAsync(SponsorTier tier, string actor)
    {
        if (string.IsNullOrWhiteSpace(tier.Name))
            return null;

        if (await _db.SponsorTierNameTakenAsync(tier.Name, 0))
            return null;

        SponsorTier created;

        try
        {
            created = await _db.CreateSponsorTierAsync(tier);
        }
        catch (DbUpdateException ex)
        {
            _sawmill.Warning($"Не удалось создать спонсорский тир '{tier.Name}': {ex.Message}");
            return null;
        }

        await ReloadTiersAndRefresh();
        Audit(actor, $"создал спонсорский тир '{created.Name}' (id {created.Id})");

        return created;
    }

    public async Task<bool> UpdateTierAsync(SponsorTier tier, string actor)
    {
        if (string.IsNullOrWhiteSpace(tier.Name))
            return false;

        if (await _db.SponsorTierNameTakenAsync(tier.Name, tier.Id))
            return false;

        try
        {
            if (!await _db.UpdateSponsorTierAsync(tier))
                return false;
        }
        catch (DbUpdateException ex)
        {
            _sawmill.Warning($"Не удалось сохранить спонсорский тир '{tier.Name}': {ex.Message}");
            return false;
        }

        await ReloadTiersAndRefresh();
        Audit(actor, $"изменил спонсорский тир '{tier.Name}' (id {tier.Id})");

        return true;
    }

    public async Task<bool> DeleteTierAsync(int tierId, string actor)
    {
        var name = _tiers.TryGetValue(tierId, out var tier) ? tier.Name : tierId.ToString();

        if (!await _db.DeleteSponsorTierAsync(tierId))
            return false;

        await ReloadTiersAndRefresh(reloadGrants: true);
        Audit(actor, $"удалил спонсорский тир '{name}' (id {tierId})");

        return true;
    }

    public async Task<SponsorGrant?> AddGrantAsync(SponsorGrant grant, string actor)
    {
        if (grant.TierId == null && grant.Overrides == null)
            return null;

        if (grant.TierId != null && !_tiers.ContainsKey(grant.TierId.Value))
            return null;

        var created = await _db.CreateSponsorGrantAsync(grant);

        await ReloadGrants(new NetUserId(grant.UserId));
        Audit(actor, $"выдал спонсорку игроку {grant.UserId} ({DescribeGrant(created)})");

        return created;
    }

    public async Task<bool> UpdateGrantAsync(SponsorGrant grant, string actor)
    {
        if (grant.TierId != null && !_tiers.ContainsKey(grant.TierId.Value))
            return false;

        if (!await _db.UpdateSponsorGrantAsync(grant))
            return false;

        await ReloadGrants(new NetUserId(grant.UserId));
        Audit(actor, $"изменил спонсорскую выдачу {grant.Id} игрока {grant.UserId} ({DescribeGrant(grant)})");

        return true;
    }

    public async Task<bool> RevokeGrantAsync(int grantId, Guid? revokedBy, string actor)
    {
        var grant = await _db.GetSponsorGrantAsync(grantId);

        if (grant == null)
            return false;

        if (!await _db.RevokeSponsorGrantAsync(grantId, revokedBy))
            return false;

        await ReloadGrants(new NetUserId(grant.UserId));
        Audit(actor, $"отозвал спонсорскую выдачу {grantId} у игрока {grant.UserId}");

        return true;
    }

    public Task<List<SponsorGrant>> GetGrantHistoryAsync(Guid userId)
    {
        return _db.GetSponsorGrantHistoryAsync(userId);
    }

    public async Task<SponsorDiscordRoleMap> GetDiscordRoleMapAsync()
    {
        var grants = await _db.GetAllSponsorGrantsAsync();
        var now = DateTime.UtcNow;

        var managed = new HashSet<string>();
        var byUser = new Dictionary<Guid, List<SponsorGrant>>();

        foreach (var tier in _tiers.Values)
        {
            managed.UnionWith(tier.Benefits.DiscordRoles);
        }

        foreach (var grant in grants)
        {
            if (grant.Overrides != null)
                managed.UnionWith(grant.Overrides.DiscordRoles);

            if (!byUser.TryGetValue(grant.UserId, out var list))
            {
                list = new List<SponsorGrant>();
                byUser[grant.UserId] = list;
            }

            list.Add(grant);
        }

        var players = new Dictionary<Guid, string[]>();

        foreach (var (userId, list) in byUser)
        {
            var data = Resolve(list, now);

            if (data.DiscordRoles.Count == 0)
                continue;

            players[userId] = data.DiscordRoles.ToArray();
        }

        return new SponsorDiscordRoleMap
        {
            Managed = managed,
            Players = players,
        };
    }

    public Task<SponsorGrant?> GetGrantAsync(int grantId)
    {
        return _db.GetSponsorGrantAsync(grantId);
    }

    private async Task ReloadTiersAndRefresh(bool reloadGrants = false)
    {
        _tiersLoad = LoadTiers();
        await _tiersLoad;

        if (!reloadGrants)
        {
            RefreshAllCached();
            return;
        }

        foreach (var userId in _cache.Keys.ToArray())
        {
            await ReloadGrants(userId);
        }
    }

    private async Task ReloadGrants(NetUserId userId)
    {
        if (!_cache.ContainsKey(userId))
            return;

        try
        {
            var grants = await _db.GetSponsorGrantsAsync(userId.UserId);

            if (!_cache.ContainsKey(userId))
                return;

            _cache[userId] = new CachedSponsor
            {
                Grants = grants,
                Data = Resolve(grants, DateTime.UtcNow),
            };

            SendState(userId);
        }
        catch (Exception ex)
        {
            _sawmill.Error($"Не удалось перечитать спонсорские выдачи для {userId}: {ex}");
        }
    }

    private void Audit(string actor, string message)
    {
        _sawmill.Info($"{actor}: {message}");
        _adminLog.Add(LogType.AdminCommands, LogImpact.High, $"Спонсорка: {actor} {message}");
    }

    private static string DescribeGrant(SponsorGrant grant)
    {
        var tier = grant.TierName ?? (grant.TierId?.ToString() ?? "без тира");
        var expires = grant.ExpiresAt == null ? "бессрочно" : grant.ExpiresAt.Value.ToString("u");
        var overrides = grant.Overrides == null ? "без надстройки" : "с персональной надстройкой";

        return $"тир: {tier}, срок: {expires}, {overrides}";
    }
}

public sealed class SponsorDiscordRoleMap
{
    public HashSet<string> Managed = new();
    public Dictionary<Guid, string[]> Players = new();
}
