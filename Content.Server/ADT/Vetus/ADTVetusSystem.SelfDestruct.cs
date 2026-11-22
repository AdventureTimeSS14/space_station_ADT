using Content.Server.ADT.Explosion;
using Content.Server.ADT.Salvage.Systems;
using Content.Server.Storage.EntitySystems;
using Content.Shared.ActionBlocker;
using Content.Shared.ADT.Explosion;
using Content.Shared.ADT.Salvage.Components;
using Content.Shared.ADT.Vetus;
using Content.Shared.Damage.Systems;
using Content.Shared.Flash;
using Content.Shared.Movement.Events;
using Content.Shared.Popups;

namespace Content.Server.ADT.Vetus;

public sealed partial class ADTVetusSystem
{
    [Dependency] private ActionBlockerSystem _blocker = default!;
    [Dependency] private ADTBlastSystem _blast = default!;
    [Dependency] private EntityStorageSystem _entityStorage = default!;
    [Dependency] private MegafaunaLootSystem _loot = default!;
    [Dependency] private SharedFlashSystem _flash = default!;

    private void InitializeSelfDestruct()
    {
        SubscribeLocalEvent<ADTVetusComponent, DamageChangedEvent>(OnVetusDamageChanged);
        SubscribeLocalEvent<ADTVetusComponent, UpdateCanMoveEvent>(OnVetusCanMove);
    }

    private void OnVetusDamageChanged(Entity<ADTVetusComponent> ent, ref DamageChangedEvent args)
    {
        if (ent.Comp.Exploding || !args.DamageIncreased)
            return;

        if (!_thresholds.TryGetDeadThreshold(ent, out var threshold))
            return;

        if (_damageable.GetTotalDamage(ent.Owner) < threshold.Value)
            return;

        StartSelfDestruct(ent);
    }

    private void OnVetusCanMove(Entity<ADTVetusComponent> ent, ref UpdateCanMoveEvent args)
    {
        if (ent.Comp.Exploding)
            args.Cancel();
    }

    private void StartSelfDestruct(Entity<ADTVetusComponent> ent)
    {
        ent.Comp.Exploding = true;

        _megafauna.Say(ent, "adt-vetus-self-destruct", 5, true);
        _popup.PopupEntity(Loc.GetString("adt-vetus-self-destruct-start", ("vetus", ent.Owner)), ent, PopupType.LargeCaution);
        _audio.PlayPvs(ent.Comp.SelfDestructAlarm, ent);

        _charge.CancelCharges(ent);
        _blocker.UpdateCanMove(ent);

        ScheduleSelfDestructBeep(ent);
        _megafauna.Schedule(ent, ent.Comp.SelfDestructDelay, () => Kaboom(ent));
    }

    private void ScheduleSelfDestructBeep(Entity<ADTVetusComponent> ent)
    {
        _megafauna.Schedule(ent, ent.Comp.SelfDestructBeepInterval, () =>
        {
            if (TerminatingOrDeleted(ent))
                return;

            _audio.PlayPvs(ent.Comp.SelfDestructBeep, ent);
            ScheduleSelfDestructBeep(ent);
        });
    }

    private void Kaboom(Entity<ADTVetusComponent> ent)
    {
        if (TerminatingOrDeleted(ent))
            return;

        var coords = _transform.GetMapCoordinates(ent);

        _popup.PopupCoordinates(Loc.GetString("adt-vetus-death", ("vetus", ent.Owner)), _transform.GetMoverCoordinates(ent), PopupType.LargeCaution);

        _flash.FlashArea(ent, null, ent.Comp.SelfDestructFlashRange, ent.Comp.SelfDestructFlashDuration);
        _blast.Explode(
            coords,
            new ADTBlastRadii(ent.Comp.SelfDestructDevastation, ent.Comp.SelfDestructHeavy, ent.Comp.SelfDestructLight),
            ent);

        EntityUid? crate = null;

        if (TryComp<MegafaunaLootComponent>(ent, out var loot))
            crate = _loot.DropLoot((ent, loot));

        if (ent.Comp.Mode is { } mode && ent.Comp.Cores.TryGetValue(mode, out var coreProto))
        {
            var core = Spawn(coreProto, _transform.GetMoverCoordinates(ent));

            if (crate is { } crateUid)
                _entityStorage.Insert(core, crateUid);
        }

        QueueDel(ent);
    }
}
