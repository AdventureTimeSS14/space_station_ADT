using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using Content.Server.Corvax.Sponsors;
using Content.Shared.ADT.Sponsors;
using Robust.Shared.Network;
using Robust.Shared.Player;
using LegacySponsorInfo = Content.Shared.Corvax.Sponsors.SponsorInfo;

namespace Content.Server.ADT.Sponsors;

public sealed partial class SponsorManager
{
    [Dependency] private SponsorsManager _legacy = default!;

    private const int LegacyGrantId = -1;

    private static readonly TimeSpan LegacySeenInterval = TimeSpan.FromDays(1);

    private bool _legacyBridge;
    private bool _legacyImport;

    private Dictionary<int, SponsorLegacyTier> _legacyTiers = new();

    private void InitializeLegacy()
    {
        _cfg.OnValueChanged(SponsorCVars.LegacyBridge, OnLegacyBridgeChanged, true);
        _cfg.OnValueChanged(SponsorCVars.LegacyImport, OnLegacyImportChanged, true);

        _legacy.InfoLoaded += OnLegacyInfoLoaded;

        LoadLegacyTiers();
    }

    private void ShutdownLegacy()
    {
        _cfg.UnsubValueChanged(SponsorCVars.LegacyBridge, OnLegacyBridgeChanged);
        _cfg.UnsubValueChanged(SponsorCVars.LegacyImport, OnLegacyImportChanged);

        _legacy.InfoLoaded -= OnLegacyInfoLoaded;
    }

    public bool LegacyImportEnabled => _legacyImport;

    public override bool IsLoadoutAllowed(ICommonSession? session, string loadoutId)
    {
        if (base.IsLoadoutAllowed(session, loadoutId))
            return true;

        return IsLegacySponsor(session);
    }

    public override bool IsMarkingAllowed(ICommonSession? session, string markingId)
    {
        if (base.IsMarkingAllowed(session, markingId))
            return true;

        if (!TryGetLegacyInfo(session, out var info))
            return false;

        return info.AllowedMarkings.Contains(markingId);
    }

    public override bool IsSpeciesAllowed(ICommonSession? session, string speciesId)
    {
        if (base.IsSpeciesAllowed(session, speciesId))
            return true;

        return IsLegacySponsor(session);
    }

    public override bool IsTraitAllowed(ICommonSession? session, string traitId)
    {
        if (base.IsTraitAllowed(session, traitId))
            return true;

        return IsLegacySponsor(session);
    }

    private bool IsLegacySponsor(ICommonSession? session)
    {
        if (!TryGetLegacyInfo(session, out var info))
            return false;

        return info.Tier > 0;
    }

    private bool TryGetLegacyInfo(ICommonSession? session, [NotNullWhen(true)] out LegacySponsorInfo? info)
    {
        info = null;

        if (!_legacyBridge || session == null)
            return false;

        if (!_legacy.TryGetRawInfo(session.UserId, out var legacyInfo))
            return false;

        if (legacyInfo.ExpireDate.ToLocalTime() <= DateTime.Now)
            return false;

        info = legacyInfo;
        return true;
    }

    private void RebuildLegacyGrant(NetUserId userId, CachedSponsor cached, DateTime nowUtc)
    {
        cached.Legacy = null;

        if (!_legacyImport)
            return;

        if (!_legacy.TryGetRawInfo(userId, out var info))
            return;

        cached.Legacy = BuildLegacyGrant(userId, info, nowUtc);
    }

    private SponsorGrant? BuildLegacyGrant(NetUserId userId, LegacySponsorInfo info, DateTime nowUtc)
    {
        var expiresAt = LegacyToUtc(info.ExpireDate);
        var expired = expiresAt <= nowUtc;

        if (!expired && info.Tier == null)
            return null;

        if (expired && !info.AllowJob)
            return null;

        var benefits = new SponsorBenefits();
        var hasOverrides = false;

        if (info.AllowJob)
        {
            benefits.RoleBypass = SponsorRoleBypass.Jobs;
            hasOverrides = true;
        }

        int? tierId = null;
        string? tierName = null;

        if (!expired)
        {
            if (TryGetMappedTier(info.Tier, out var tier))
            {
                tierId = tier.Id;
                tierName = tier.Name;
            }

            var markings = info.AllowedMarkings ?? Array.Empty<string>();

            if (markings.Length > 0)
            {
                benefits.Markings.UnionWith(markings);
                benefits.TtsVoices.UnionWith(markings);
                hasOverrides = true;
            }

            if (info.ExtraSlots > 0)
            {
                benefits.ExtraCharacterSlots = info.ExtraSlots;
                hasOverrides = true;
            }

            if (info.HavePriorityJoin)
            {
                benefits.PriorityJoin = true;
                hasOverrides = true;
            }

            if (!string.IsNullOrEmpty(info.OOCColor) && Color.TryFromHex(info.OOCColor, out var color))
            {
                benefits.OocColor = color;
                hasOverrides = true;
            }
        }

        if (tierId == null && !hasOverrides)
            return null;

        return new SponsorGrant
        {
            Id = LegacyGrantId,
            UserId = userId.UserId,
            TierId = tierId,
            TierName = tierName,
            Overrides = hasOverrides ? benefits : null,
            Comment = info.Tier == null
                ? "Старая спонсорка"
                : $"Старая спонсорка, тир {info.Tier}",
            CreatedAt = nowUtc,
            ExpiresAt = expired ? null : expiresAt,
            Legacy = true,
        };
    }

    private bool TryGetMappedTier(int? legacyTier, [NotNullWhen(true)] out SponsorTier? tier)
    {
        tier = null;

        if (legacyTier == null)
            return false;

        if (!_legacyTiers.TryGetValue(legacyTier.Value, out var mapping) || mapping.TierId == null)
            return false;

        return _tiers.TryGetValue(mapping.TierId.Value, out tier);
    }

    private static DateTime LegacyToUtc(DateTime value)
    {
        if (value.Kind == DateTimeKind.Local)
            return value.ToUniversalTime();

        return DateTime.SpecifyKind(value, DateTimeKind.Utc);
    }

    private void OnLegacyInfoLoaded(NetUserId userId)
    {
        if (_legacy.TryGetRawInfo(userId, out var info))
            MarkLegacyTierSeen(info.Tier);

        if (_cache.ContainsKey(userId))
            RefreshCached(userId);
    }

    private void MarkLegacyTierSeen(int? legacyTier)
    {
        if (legacyTier == null)
            return;

        var now = DateTime.UtcNow;

        if (_legacyTiers.TryGetValue(legacyTier.Value, out var entry)
            && entry.LastSeen != null
            && now - entry.LastSeen.Value < LegacySeenInterval)
        {
            return;
        }

        if (entry == null)
        {
            entry = new SponsorLegacyTier
            {
                LegacyTier = legacyTier.Value,
            };

            _legacyTiers[legacyTier.Value] = entry;
        }

        var previous = entry.LastSeen;
        entry.LastSeen = now;

        SaveLegacyTierSeen(entry, previous, now);
    }

    private async void SaveLegacyTierSeen(SponsorLegacyTier entry, DateTime? previous, DateTime seenUtc)
    {
        try
        {
            await _db.MarkSponsorLegacyTierSeenAsync(entry.LegacyTier, seenUtc);
        }
        catch (Exception ex)
        {
            if (entry.LastSeen == seenUtc)
                entry.LastSeen = previous;

            _sawmill.Error($"Не удалось отметить старый спонсорский тир {entry.LegacyTier}: {ex}");
        }
    }

    private async void LoadLegacyTiers()
    {
        try
        {
            var loaded = await _db.GetSponsorLegacyTiersAsync();
            var tiers = new Dictionary<int, SponsorLegacyTier>();

            foreach (var tier in loaded)
            {
                tiers[tier.LegacyTier] = tier;
            }

            foreach (var (legacyTier, seen) in _legacyTiers)
            {
                if (!tiers.TryGetValue(legacyTier, out var tier))
                {
                    tiers[legacyTier] = seen;
                    continue;
                }

                if (tier.LastSeen == null || seen.LastSeen > tier.LastSeen)
                    tier.LastSeen = seen.LastSeen;
            }

            _legacyTiers = tiers;

            await _tiersLoad;
            RefreshAllCached();
        }
        catch (Exception ex)
        {
            _sawmill.Error($"Не удалось загрузить соответствие старых спонсорских тиров: {ex}");
        }
    }

    public async Task<List<SponsorLegacyTier>> GetLegacyTiersAsync()
    {
        var loaded = await _db.GetSponsorLegacyTiersAsync();
        var result = new Dictionary<int, SponsorLegacyTier>();

        foreach (var tier in loaded)
        {
            result[tier.LegacyTier] = tier;
        }

        foreach (var (legacyTier, known) in _legacyTiers)
        {
            if (result.ContainsKey(legacyTier))
                continue;

            result[legacyTier] = new SponsorLegacyTier
            {
                LegacyTier = legacyTier,
                TierId = known.TierId,
                LastSeen = known.LastSeen,
            };
        }

        foreach (var session in _players.Sessions)
        {
            if (!_legacy.TryGetRawInfo(session.UserId, out var info) || info.Tier == null)
                continue;

            if (!result.TryGetValue(info.Tier.Value, out var tier))
            {
                tier = new SponsorLegacyTier
                {
                    LegacyTier = info.Tier.Value,
                };

                result[info.Tier.Value] = tier;
            }

            tier.OnlinePlayers++;
        }

        return result.Values.OrderBy(t => t.LegacyTier).ToList();
    }

    public async Task<bool> SetLegacyTierAsync(int legacyTier, int? tierId, string actor)
    {
        if (tierId != null && !_tiers.ContainsKey(tierId.Value))
            return false;

        await _db.SaveSponsorLegacyTierAsync(legacyTier, tierId);

        if (!_legacyTiers.TryGetValue(legacyTier, out var entry))
        {
            entry = new SponsorLegacyTier
            {
                LegacyTier = legacyTier,
            };

            _legacyTiers[legacyTier] = entry;
        }

        entry.TierId = tierId;

        var tierName = tierId != null && _tiers.TryGetValue(tierId.Value, out var tier)
            ? tier.Name
            : "не сопоставлен";

        Audit(actor, $"сопоставил старый спонсорский тир {legacyTier} -> {tierName}");
        RefreshAllCached();

        return true;
    }

    public async Task<bool> DeleteLegacyTierAsync(int legacyTier, string actor)
    {
        if (!await _db.DeleteSponsorLegacyTierAsync(legacyTier))
            return false;

        _legacyTiers.Remove(legacyTier);

        Audit(actor, $"удалил старый спонсорский тир {legacyTier} из списка миграции");
        RefreshAllCached();

        return true;
    }

    public async Task<SponsorGrant?> GetLegacyGrantAsync(NetUserId userId)
    {
        if (_cache.TryGetValue(userId, out var cached) && cached.Legacy != null)
            return cached.Legacy.Clone();

        if (_legacy.TryGetRawInfo(userId, out var info))
            return BuildLegacyGrant(userId, info, DateTime.UtcNow);

        if (_players.TryGetSessionById(userId, out _))
            return null;

        var fetched = await _legacy.FetchInfoAsync(userId);

        if (fetched == null)
            return null;

        return BuildLegacyGrant(userId, fetched, DateTime.UtcNow);
    }

    private void OnLegacyBridgeChanged(bool value)
    {
        _legacyBridge = value;

        if (!value)
            _sawmill.Info("Мост на старую спонсорку выключен.");
    }

    private void OnLegacyImportChanged(bool value)
    {
        if (_legacyImport == value)
            return;

        _legacyImport = value;

        _sawmill.Info(value
            ? "Импорт старой спонсорки включён."
            : "Импорт старой спонсорки выключен.");

        RefreshAllCached();
    }
}
