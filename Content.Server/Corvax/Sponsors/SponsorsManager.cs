using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Content.Server.ADT.SponsorLoadout;
using Content.Server.Database;
using Content.Shared.Corvax.CCCVars;
using Content.Shared.Corvax.Sponsors;
using Content.Shared.Roles;
using Robust.Server.Player;
using Robust.Shared.Configuration;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Server.Corvax.Sponsors;

public sealed class SponsorsManager
{
    [Dependency] private readonly IServerNetManager _netMgr = default!;
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly IServerDbManager _dbManager = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;

    private readonly HttpClient _httpClient = new();

    private ISawmill _sawmill = default!;
    private string _apiUrl = string.Empty;

    private readonly Dictionary<NetUserId, SponsorInfo> _cachedSponsors = new();

    public void Initialize()
    {
        _sawmill = Logger.GetSawmill("sponsors");
        _cfg.OnValueChanged(CCCVars.SponsorsApiUrl, s => _apiUrl = s, true);

        _netMgr.RegisterNetMessage<MsgSponsorInfo>();

        _netMgr.Connecting += OnConnecting;
        _netMgr.Connected += OnConnected;
        _netMgr.Disconnect += OnDisconnect;
    }

    public bool TryGetInfo(NetUserId userId, [NotNullWhen(true)] out SponsorInfo? sponsor)
    {
        return _cachedSponsors.TryGetValue(userId, out sponsor);
    }

    private async Task OnConnecting(NetConnectingArgs e)
    {
        var info = await LoadSponsorInfo(e.UserId);

        if (info == null)
        {
            _cachedSponsors.Remove(e.UserId);
            return;
        }

        var isExpired = info.ExpireDate.ToLocalTime() <= DateTime.Now;

        if (isExpired && info.AllowJob)
        {
            info = new SponsorInfo
            {
                CharacterName = info.CharacterName,
                Tier = null,
                OOCColor = null,
                HavePriorityJoin = false,
                ExtraSlots = 0,
                AllowedMarkings = Array.Empty<string>(),
                ExpireDate = info.ExpireDate,
                AllowJob = true
            };
        }

        else if (isExpired || info.Tier == null)
        {
            _cachedSponsors.Remove(e.UserId);
            return;
        }

        DebugTools.Assert(!_cachedSponsors.ContainsKey(e.UserId), "Cached data was found on client connect");

        _cachedSponsors[e.UserId] = info;
    }

    private void OnConnected(object? sender, NetChannelArgs e)
    {
        var info = _cachedSponsors.TryGetValue(e.Channel.UserId, out var sponsor) ? sponsor : null;
        var msg = new MsgSponsorInfo() { Info = info };
        _netMgr.ServerSendMessage(msg, e.Channel);
    }

    private void OnDisconnect(object? sender, NetDisconnectedArgs e)
    {
        _cachedSponsors.Remove(e.Channel.UserId);
    }

    private async Task<SponsorInfo?> LoadSponsorInfo(NetUserId userId)
    {
        if (!string.IsNullOrEmpty(_apiUrl))
        {
            try // ADT TWEAK
            {
                var url = $"{_apiUrl}/sponsors/{userId.ToString()}";
                var response = await _httpClient.GetAsync(url);

                if (response.StatusCode == HttpStatusCode.NotFound)
                    return null;

                if (response.StatusCode != HttpStatusCode.OK)
                {
                    var errorText = await response.Content.ReadAsStringAsync();
                    _sawmill.Error(
                        "Failed to get player sponsor OOC color from API: [{StatusCode}] {Response}",
                        response.StatusCode,
                        errorText);
                    return null;
                }

                return await response.Content.ReadFromJsonAsync<SponsorInfo>();
            }
            catch (HttpRequestException) // ADT TWEAK
            {
                _sawmill.Error("No internet connection or network error while fetching sponsor info.");
                return null;
            }
        }

        return null;
    }
    // ADT-Tweak-start: add round start sponsor loadouts
    public bool TryGetSpawnEquipment(NetUserId userId, string? jobPrototype, [NotNullWhen(true)] out string? spawnEquipment)
    {
        spawnEquipment = null;

        // // ТЕСТОВЫЕ ДАННЫЕ - НАЧАЛО (удалить в мастере) (ИМИТАЦИЯ СПОНСОРКИ)
        // var sponsorData = new SponsorInfo
        // {
        //     CharacterName = "TestSponsor",
        //     Tier = 4,
        //     OOCColor = "#FF0000",
        //     HavePriorityJoin = true,
        //     ExtraSlots = 2,
        //     AllowedMarkings = new[] { "marking1", "marking2" },
        //     ExpireDate = DateTime.Now.AddDays(30),
        //     AllowJob = true
        // };
        // // ТЕСТОВЫЕ ДАННЫЕ - КОНЕЦ

        // Получаем sponsorData юсера
        if (!TryGetInfo(userId, out var sponsorData))
        {
            return false;
        }

        // Попытка найти персональный набор
        if (_playerManager.TryGetSessionById(userId, out var session))
        {
            var username = session.Name;
            var personalGears = _prototypeManager.EnumeratePrototypes<SponsorPersonalLoadoutPrototype>();
            var currentDate = DateTime.UtcNow;

            // 1. Сначала ищем лоадаут по должности
            var jobLoadout = personalGears.FirstOrDefault(loadout =>
                loadout.UserName == username &&
                jobPrototype != null &&
                loadout.WhitelistJobs?.Contains(jobPrototype) == true &&
                (loadout.ExpirationDate == null || loadout.ExpirationDate > currentDate));

            if (jobLoadout != null)
            {
                spawnEquipment = jobLoadout.Equipment;
                return true;
            }

            // 2. Если нет подходящего по должности, берём общий персональный
            var generalLoadout = personalGears.FirstOrDefault(loadout =>
                loadout.UserName == username &&
                (loadout.WhitelistJobs == null || loadout.WhitelistJobs.Count == 0) &&
                (loadout.ExpirationDate == null || loadout.ExpirationDate > currentDate));

            if (generalLoadout != null)
            {
                spawnEquipment = generalLoadout.Equipment;
                return true;
            }
        }

        // Если персонального лоадаута нет — проверяем Tier
        var tierSettings = _prototypeManager.EnumeratePrototypes<SponsorLoadoutTierSettingPrototype>().FirstOrDefault();
        if (
            tierSettings != null &&
            sponsorData.Tier.HasValue &&
            tierSettings.Tiers.TryGetValue(sponsorData.Tier.Value, out var equipmentId)
        )
        {
            spawnEquipment = equipmentId;
            return true;
        }

        return spawnEquipment != null;
    }
    // ADT-Tweak-End
}
