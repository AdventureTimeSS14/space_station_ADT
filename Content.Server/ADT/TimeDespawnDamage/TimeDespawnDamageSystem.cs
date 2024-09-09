using Content.Server.Chat.Systems;
using Content.Shared.ADT.TimeDespawnDamage;
using Robust.Shared.Timing;
using Robust.Shared.Random;
using Content.Shared.Damage;
using Content.Server.Body.Components;
using Content.Shared.Chemistry.Components;
using Content.Server.Fluids.EntitySystems;
using Content.Shared.Mobs.Systems;
using Content.Shared.ADT.Silicon.Components;
using Content.Shared.Damage.Prototypes;
using Robust.Shared.Prototypes;
using Robust.Shared.Audio.Systems;
using System.Threading;
using Robust.Shared.Spawners;
using Robust.Shared.Audio;
using Content.Server.Administration.Logs;
using Content.Server.Administration.Managers;
using Content.Server.Administration.UI;
using Content.Server.Disposal.Tube;
using Content.Server.Disposal.Tube.Components;
using Content.Server.EUI;
using Content.Server.GameTicking;
using Content.Server.Ghost.Roles;
using Content.Server.Mind;
using Content.Server.Mind.Commands;
using Content.Server.Prayer;
using Content.Server.Station.Systems;
using Content.Server.Xenoarchaeology.XenoArtifacts;
using Content.Server.Xenoarchaeology.XenoArtifacts.Triggers.Components;
using Content.Shared.Administration;
using Content.Shared.Chemistry.Components.SolutionManager;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Configurable;
using Content.Shared.Database;
using Content.Shared.Examine;
using Content.Shared.GameTicking;
using Content.Shared.Inventory;
using Content.Shared.Mind.Components;
using Content.Shared.Popups;
using Content.Shared.Verbs;
using Robust.Server.Console;
using Robust.Server.GameObjects;
using Robust.Shared.Console;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;
using Robust.Shared.Toolshed;
using Robust.Shared.Utility;
using System.Linq;
using Content.Server.Silicons.Laws;
using Content.Shared.Silicons.Laws;
using Content.Shared.Silicons.Laws.Components;
using Robust.Server.Player;
using Content.Shared.Mind;
using Robust.Shared.Physics.Components;
using static Content.Shared.Configurable.ConfigurationComponent;
using Content.Server.Administration.Systems;
using Content.Shared.IdentityManagement;
using System.Linq;
using Content.Server.Administration.Managers;
using Content.Server.Chat.Managers;
using Content.Server.Corvax.Sponsors;
using Content.Server.Forensics;
using Content.Server.GameTicking;
using Content.Server.Hands.Systems;
using Content.Server.Mind;
using Content.Server.Players.PlayTimeTracking;
using Content.Server.Popups;
using Content.Server.StationRecords.Systems;
using Content.Shared.Administration;
using Content.Shared.Administration.Events;
using Content.Shared.CCVar;
using Content.Shared.Corvax.CCCVars;
using Content.Shared.Corvax.Sponsors;
using Content.Shared.GameTicking;
using Content.Shared.Hands.Components;
using Content.Shared.IdentityManagement;
using Content.Shared.Inventory;
using Content.Shared.Mind;
using Content.Shared.PDA;
using Content.Shared.Players;
using Content.Shared.Players.PlayTimeTracking;
using Content.Shared.Popups;
using Content.Shared.Roles;
using Content.Shared.Roles.Jobs;
using Content.Shared.StationRecords;
using Content.Shared.Throwing;
using Robust.Server.GameObjects;
using Robust.Server.Player;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Configuration;
using Robust.Shared.Enums;
using Robust.Shared.Network;
using Robust.Shared.Player;

public sealed class TimeDespawnDamageSystem : EntitySystem
{
    [Dependency] private readonly MindSystem _mindSystem = default!;
    [Dependency] private readonly AdminSystem _adminSystem = default!;
    [Dependency] private readonly IGameTiming _time = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly PuddleSystem _puddleSystem = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly TransformSystem _transform = default!;
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly InventorySystem _inventory = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly IAdminManager _adminManager = default!;
    [Dependency] private readonly IChatManager _chat = default!;
    [Dependency] private readonly IConfigurationManager _config = default!;
    [Dependency] private readonly HandsSystem _hands = default!;
    [Dependency] private readonly SharedJobSystem _jobs = default!;
    [Dependency] private readonly MindSystem _minds = default!;
    [Dependency] private readonly PhysicsSystem _physics = default!;
    [Dependency] private readonly PlayTimeTrackingManager _playTime = default!;
    [Dependency] private readonly SharedRoleSystem _role = default!;
    [Dependency] private readonly GameTicker _gameTicker = default!;
    [Dependency] private readonly StationRecordsSystem _stationRecords = default!;


    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<TimeDespawnDamageComponent, DamageChangedEvent>(OnMobStateDamage);
    }

    private void OnMobStateDamage(EntityUid uid, TimeDespawnDamageComponent component, DamageChangedEvent args)
    {
        if (EntityManager.TryGetComponent<DamageableComponent>(uid, out var damageable))
        {
            var damagePerGroup = damageable.Damage.GetDamagePerGroup(_prototypeManager);
            if (damagePerGroup.TryGetValue("ADTTime", out var timeDamage))
            {
                Log.Debug($"Сущности {ToPrettyString(uid)} нанесли: '{timeDamage}' временного дамага.");

                if (timeDamage > 7)
                {
                    if (TryComp<DamageableComponent>(uid, out var _))
                    {
                        var igniteSound = new SoundPathSpecifier("/Audio/Magic/forcewall.ogg");
                        // var despawn = AddComp<TimedDespawnComponent>(uid);
                        // despawn.Lifetime = 1.1f;
                        //Thread.Sleep(2000); //2 секунды задержка
                        //AddComp<SingularityDistortionComponent>(uid);
                        Log.Debug($"Сущность нанесли: '{timeDamage}' временного дамага и она СТЁРЛАСЬ.");
                        EraseDeleteTime(uid);
                        _audio.PlayPvs(igniteSound, uid);
                    }
                }
            }
        }
        else
        {
            Log.Debug($"Сущность {ToPrettyString(uid)} не имеет компонента DamageableComponent.");
        }
    }

    private void EraseDeleteTime(EntityUid uid)
    {
        var entity = uid;

        if (TryComp(entity, out TransformComponent? transform))
        {
            var coordinates = _transform.GetMoverCoordinates(entity, transform);
            var name = Identity.Entity(entity, EntityManager);
            _popup.PopupCoordinates(Loc.GetString("admin-erase-popup", ("user", name)), coordinates, PopupType.LargeCaution);
            var filter = Filter.Pvs(coordinates, 1, EntityManager, _playerManager);
            var audioParams = new AudioParams().WithVolume(3);
            _audio.PlayStatic("/Audio/Magic/forcewall.ogg", filter, coordinates, true, audioParams);
        }

        foreach (var item in _inventory.GetHandOrInventoryEntities(entity))
        {
            if (TryComp(item, out PdaComponent? pda) &&
                TryComp(pda.ContainedId, out StationRecordKeyStorageComponent? keyStorage) &&
                keyStorage.Key is { } key &&
                _stationRecords.TryGetRecord(key, out GeneralStationRecord? record))
            {
                if (TryComp(entity, out DnaComponent? dna) &&
                    dna.DNA != record.DNA)
                {
                    continue;
                }

                if (TryComp(entity, out FingerprintComponent? fingerPrint) &&
                    fingerPrint.Fingerprint != record.Fingerprint)
                {
                    continue;
                }

                _stationRecords.RemoveRecord(key);
                Del(item);
            }
        }

        if (_inventory.TryGetContainerSlotEnumerator(entity, out var enumerator))
        {
            while (enumerator.NextItem(out var item, out var slot))
            {
                if (_inventory.TryUnequip(entity, entity, slot.Name, true, true))
                    _physics.ApplyAngularImpulse(item, ThrowingSystem.ThrowAngularImpulse);
            }
        }

        if (TryComp(entity, out HandsComponent? hands))
        {
            foreach (var hand in _hands.EnumerateHands(entity, hands))
            {
                _hands.TryDrop(entity, hand, checkActionBlocker: false, doDropInteraction: false, handsComp: hands);
            }
        }
        QueueDel(entity);

        // if (_playerManager.TryGetSessionById(uid, out var session))
        //     _gameTicker.SpawnObserver(session);

    }
}
