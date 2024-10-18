using Content.Shared.ADT.TimeDespawnDamage;
using Content.Shared.Damage;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Audio;
using Content.Shared.Inventory;
using Content.Shared.Popups;
using Robust.Server.GameObjects;
using Robust.Shared.Player;
using Robust.Server.Player;
using Content.Shared.IdentityManagement;
using Content.Server.Forensics;
using Content.Server.Hands.Systems;
using Content.Server.Popups;
using Content.Server.StationRecords.Systems;
using Content.Shared.Hands.Components;
using Content.Shared.PDA;
using Content.Shared.StationRecords;
using Content.Shared.Throwing;
public sealed class TimeDespawnDamageSystem : EntitySystem
{
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly TransformSystem _transform = default!;
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly InventorySystem _inventory = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly HandsSystem _hands = default!;
    [Dependency] private readonly PhysicsSystem _physics = default!;
    [Dependency] private readonly StationRecordsSystem _stationRecords = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<TimeDespawnDamageComponent, DamageChangedEvent>(OnMobStateDamage);
    }

    private void OnMobStateDamage(EntityUid uid, TimeDespawnDamageComponent component, DamageChangedEvent args)
    {
        if (component.Count >= 7)
        {
            var igniteSound = new SoundPathSpecifier("/Audio/Magic/forcewall.ogg");
            EraseDeleteTime(uid);
            _audio.PlayPvs(igniteSound, uid);
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
