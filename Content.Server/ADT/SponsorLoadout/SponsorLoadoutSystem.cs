using Content.Server.Corvax.Sponsors;
using Content.Server.Station.Systems;
using Content.Shared.GameTicking;
using Robust.Shared.Prototypes;

namespace Content.Server.ADT.SponsorLoadout;

public sealed class SponsorLoadoutSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly StationSpawningSystem _spawn = default!;
    [Dependency] private readonly SponsorsManager _sponsorsManager = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<PlayerSpawnCompleteEvent>(OnPlayerSpawned);
    }

    private void OnPlayerSpawned(PlayerSpawnCompleteEvent ev)
    {
        if (_sponsorsManager == null)
            return;

        // Получаем экипировку (может быть персональной или по Tier)
        if (!_sponsorsManager.TryGetSpawnEquipment(ev.Player.UserId, ev.JobId, out var spawnEquipment))
            return;

        // Проверяем, является ли лоадаут персональным
        if (_prototypeManager.TryIndex<SponsorPersonalLoadoutPrototype>(spawnEquipment, out var personalLoadout))
        {
            EquipLoadout(ev, personalLoadout);
            return;
        }

        // Проверяем, является ли лоадаут для обычного tier
        if (_prototypeManager.TryIndex<SponsorLoadoutPrototype>(spawnEquipment, out var loadout))
        {
            EquipLoadout(ev, loadout);
            return;
        }
    }

    // Универсальный метод для экипировки лоадаута
    private void EquipLoadout<T>(PlayerSpawnCompleteEvent ev, T loadout) where T : IPrototype
    {
        if (loadout is SponsorLoadoutPrototype sponsorLoadout)
        {
            if (IsRestricted(ev, sponsorLoadout.WhitelistJobs, sponsorLoadout.BlacklistJobs, sponsorLoadout.SpeciesRestrictions))
                return;

            if (!_prototypeManager.TryIndex(sponsorLoadout.Equipment, out var startingGear))
                return;

            _spawn.EquipStartingGear(ev.Mob, startingGear);
        }
        else if (loadout is SponsorPersonalLoadoutPrototype personalLoadout)
        {
            if (IsRestricted(ev, personalLoadout.WhitelistJobs, personalLoadout.BlacklistJobs, personalLoadout.SpeciesRestrictions))
                return;

            if (!_prototypeManager.TryIndex(personalLoadout.Equipment, out var startingGear))
                return;

            _spawn.EquipStartingGear(ev.Mob, startingGear);
        }
    }

    // Проверка ограничений
    private bool IsRestricted(PlayerSpawnCompleteEvent ev, List<string>? whitelist, List<string>? blacklist, List<string>? speciesRestrictions)
    {
        return (ev.JobId != null && whitelist != null && !whitelist.Contains(ev.JobId)) ||
            (ev.JobId != null && blacklist != null && blacklist.Contains(ev.JobId)) ||
            (speciesRestrictions != null && speciesRestrictions.Contains(ev.Profile.Species));
    }
}
