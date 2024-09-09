using Content.Shared.ADT.TimeDespawnDamage;
using Robust.Shared.Timing;
using Robust.Shared.Random;
using Content.Shared.Damage;
using Content.Server.Fluids.EntitySystems;
using Content.Shared.Mobs.Systems;
using Robust.Shared.Prototypes;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Audio;
using Content.Server.Administration.Managers;
using Content.Server.GameTicking;
using Content.Server.Mind;
using Content.Shared.Inventory;
using Content.Shared.Popups;
using Robust.Server.GameObjects;
using Robust.Shared.Player;
using Robust.Server.Player;
using Content.Server.Administration.Systems;
using Content.Shared.IdentityManagement;
using Content.Server.Chat.Managers;
using Content.Server.Forensics;
using Content.Server.Hands.Systems;
using Content.Server.Players.PlayTimeTracking;
using Content.Server.Popups;
using Content.Server.StationRecords.Systems;
using Content.Shared.Hands.Components;
using Content.Shared.PDA;
using Content.Shared.Roles;
using Content.Shared.Roles.Jobs;
using Content.Shared.StationRecords;
using Content.Shared.Throwing;
using Robust.Shared.Configuration;

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
                //Log.Debug($"Сущности {ToPrettyString(uid)} нанесли: '{timeDamage}' временного дамага.");

                if (timeDamage > 6)
                {
                    if (TryComp<DamageableComponent>(uid, out var _))
                    {
                        var igniteSound = new SoundPathSpecifier("/Audio/Magic/forcewall.ogg");
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
    }
}
