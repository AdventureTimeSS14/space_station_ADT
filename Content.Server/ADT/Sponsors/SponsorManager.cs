using System.Collections.Frozen;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using Content.Server.Database;
using Content.Shared.ADT.Sponsors;
using Robust.Server.Player;
using Robust.Shared.Configuration;
using Robust.Shared.Network;
using Robust.Shared.Player;

namespace Content.Server.ADT.Sponsors;

/// <summary>
/// Единственный авторитетный источник спонсорских данных на сервере
/// </summary>
public sealed partial class SponsorManager : SharedSponsorManager
{
    [Dependency] private readonly IServerNetManager _netMgr = default!;
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly IServerDbManager _db = default!;
    [Dependency] private readonly IPlayerManager _players = default!;
    [Dependency] private readonly ILogManager _logManager = default!;

    private ISawmill _sawmill = default!;

    private bool _enabled;

    private FrozenDictionary<int, SponsorTier> _tiers = FrozenDictionary<int, SponsorTier>.Empty;
    private FrozenDictionary<string, SponsorTier> _tiersByName = FrozenDictionary<string, SponsorTier>.Empty;

    private Task _tiersLoad = Task.CompletedTask;

    private readonly Dictionary<NetUserId, CachedSponsor> _cache = new();

    private sealed class CachedSponsor
    {
        public required List<SponsorGrant> Grants;
        public required SponsorData Data;
    }

    public override void Initialize()
    {
        base.Initialize();

        _sawmill = _logManager.GetSawmill("sponsors.adt");

        _cfg.OnValueChanged(SponsorCVars.Enabled, OnEnabledChanged, true);
        _cfg.OnValueChanged(SponsorCVars.LegacyBridge, OnLegacyBridgeChanged, true);

        _netMgr.RegisterNetMessage<MsgSponsorState>();
        _netMgr.RegisterNetMessage<MsgSetSponsorColors>(OnSetColors);

        RegisterColorRateLimit();

        _netMgr.Connecting += OnConnecting;
        _netMgr.Connected += OnConnected;
        _netMgr.Disconnect += OnDisconnect;

        _tiersLoad = LoadTiers();
    }

    public override void Shutdown()
    {
        base.Shutdown();

        _cfg.UnsubValueChanged(SponsorCVars.Enabled, OnEnabledChanged);
        _cfg.UnsubValueChanged(SponsorCVars.LegacyBridge, OnLegacyBridgeChanged);

        _netMgr.Connecting -= OnConnecting;
        _netMgr.Connected -= OnConnected;
        _netMgr.Disconnect -= OnDisconnect;
    }

    /// <inheritdoc/>
    public override SponsorData GetData(ICommonSession? session)
    {
        if (session == null)
            return SponsorData.Empty;

        return GetData(session.UserId);
    }

    public SponsorData GetData(NetUserId userId)
    {
        if (!_enabled)
            return SponsorData.Empty;

        if (!_cache.TryGetValue(userId, out var cached))
            return SponsorData.Empty;

        var now = DateTime.UtcNow;

        if (cached.Data.NextExpiry == null || cached.Data.NextExpiry.Value > now)
            return cached.Data;

        cached.Data = Resolve(cached.Grants, now);

        SendState(userId);
        return cached.Data;
    }

    public bool TryGetData(NetUserId userId, out SponsorData data)
    {
        data = GetData(userId);
        return data.HasAnyBenefit;
    }

    public IReadOnlyCollection<SponsorTier> Tiers => _tiers.Values;

    public bool TryGetTier(int tierId, [NotNullWhen(true)] out SponsorTier? tier)
    {
        return _tiers.TryGetValue(tierId, out tier);
    }

    public bool TryGetTierByName(string name, [NotNullWhen(true)] out SponsorTier? tier)
    {
        return _tiersByName.TryGetValue(name, out tier);
    }

    private async Task OnConnecting(NetConnectingArgs e)
    {
        if (!_enabled)
            return;

        await LoadPlayer(e.UserId);
    }

    public async Task<SponsorData> EnsureLoadedAsync(NetUserId userId)
    {
        if (!_enabled)
            return SponsorData.Empty;

        if (!_cache.ContainsKey(userId))
            await LoadPlayer(userId);

        return GetData(userId);
    }

    private async Task LoadPlayer(NetUserId userId)
    {
        await _tiersLoad;

        try
        {
            var grants = await _db.GetSponsorGrantsAsync(userId.UserId);
            _cache[userId] = new CachedSponsor
            {
                Grants = grants,
                Data = Resolve(grants, DateTime.UtcNow),
            };
        }
        catch (Exception ex)
        {
            _sawmill.Error($"Не удалось загрузить спонсорские выдачи для {userId}: {ex}");
            _cache.Remove(userId);
        }

        await LoadColors(userId);
    }

    private void OnConnected(object? sender, NetChannelArgs e)
    {
        SendState(e.Channel);
    }

    private void OnDisconnect(object? sender, NetDisconnectedArgs e)
    {
        _cache.Remove(e.Channel.UserId);
        _colors.Remove(e.Channel.UserId);
    }

    private void SendState(INetChannel channel)
    {
        var data = GetData(channel.UserId);
        var msg = new MsgSponsorState();

        if (data.HasAnyBenefit && _cache.TryGetValue(channel.UserId, out var cached))
        {
            var (ooc, ghost) = GetEffectiveColors(channel.UserId);

            msg.State = new SponsorStatePayload
            {
                Benefits = BuildClientBenefits(cached.Grants, DateTime.UtcNow),
                Tiers = data.Tiers.ToArray(),
                NextExpiry = data.NextExpiry,
                SelectedOocColor = ooc,
                SelectedGhostColor = ghost,
            };
        }

        _netMgr.ServerSendMessage(msg, channel);
    }

    private void SendState(NetUserId userId)
    {
        if (!_players.TryGetSessionById(userId, out var session))
            return;

        SendState(session.Channel);
    }

    private SponsorData Resolve(List<SponsorGrant> grants, DateTime nowUtc)
    {
        var layers = BuildLayers(grants, nowUtc, out var tiers, out var nextExpiry);

        if (layers.Count == 0)
            return SponsorData.Empty;

        return SponsorData.FromBenefits(SponsorBenefits.Merge(layers), nextExpiry, tiers);
    }

    private List<SponsorBenefitLayer> BuildLayers(
        List<SponsorGrant> grants,
        DateTime nowUtc,
        out List<SponsorTierSummary> tiers,
        out DateTime? nextExpiry)
    {
        var layers = new List<SponsorBenefitLayer>();
        tiers = new List<SponsorTierSummary>();
        nextExpiry = null;

        foreach (var grant in grants)
        {
            if (!grant.IsActive(nowUtc))
                continue;

            var contributed = false;
            var tierPriority = 0;

            if (grant.TierId != null && _tiers.TryGetValue(grant.TierId.Value, out var tier) && tier.Enabled)
            {
                tierPriority = tier.Priority;
                layers.Add(new SponsorBenefitLayer(tier.Benefits, tierPriority + grant.Priority));
                tiers.Add(new SponsorTierSummary
                {
                    Name = tier.Name,
                    DisplayName = tier.DisplayName,
                    ExpiresAt = grant.ExpiresAt,
                });
                contributed = true;
            }

            if (grant.Overrides != null)
            {
                var priority = tierPriority + grant.Priority + SponsorGrant.OverridePriorityBonus;
                layers.Add(new SponsorBenefitLayer(grant.Overrides, priority));
                contributed = true;
            }

            if (contributed && grant.ExpiresAt != null && (nextExpiry == null || grant.ExpiresAt < nextExpiry))
                nextExpiry = grant.ExpiresAt;
        }

        return layers;
    }

    private SponsorBenefits BuildClientBenefits(List<SponsorGrant> grants, DateTime nowUtc)
    {
        var layers = BuildLayers(grants, nowUtc, out _, out _);
        return SponsorBenefits.Merge(layers);
    }

    private async Task LoadTiers()
    {
        try
        {
            var tiers = await _db.GetAllSponsorTiersAsync();

            _tiers = tiers.ToFrozenDictionary(t => t.Id);
            _tiersByName = tiers.ToFrozenDictionary(t => t.Name);

            _sawmill.Info($"Загружено спонсорских тиров: {tiers.Count}.");
        }
        catch (Exception ex)
        {
            _sawmill.Error($"Не удалось загрузить спонсорские тиры: {ex}");
        }
    }

    private void RefreshCached(NetUserId userId)
    {
        if (!_cache.TryGetValue(userId, out var cached))
            return;

        cached.Data = Resolve(cached.Grants, DateTime.UtcNow);
        SendState(userId);
    }

    private void RefreshAllCached()
    {
        var users = _cache.Keys.ToArray();

        foreach (var userId in users)
        {
            RefreshCached(userId);
        }
    }

    private void OnEnabledChanged(bool value)
    {
        var wasEnabled = _enabled;
        _enabled = value;

        if (!value)
        {
            _sawmill.Info("Новая спонсорская система выключена.");
            return;
        }

        if (wasEnabled)
            return;

        LoadConnectedPlayers();
    }

    private async void LoadConnectedPlayers()
    {
        try
        {
            foreach (var session in _players.Sessions.ToArray())
            {
                if (!_enabled)
                    return;

                if (_cache.ContainsKey(session.UserId))
                    continue;

                await LoadPlayer(session.UserId);
                SendState(session.UserId);
            }
        }
        catch (Exception ex)
        {
            _sawmill.Error($"Не удалось догрузить спонсорские выдачи после включения системы: {ex}");
        }
    }
}
