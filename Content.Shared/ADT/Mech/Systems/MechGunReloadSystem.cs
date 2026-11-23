using Content.Shared.ADT.Mech;
using Content.Shared.ADT.Mech.Components;
using Content.Shared.ADT.Weapons.Ranged.Components;
using Content.Shared.Examine;
using Content.Shared.Mech;
using Content.Shared.Mech.Components;
using Content.Shared.Popups;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Timing;

namespace Content.Shared.ADT.Mech.EntitySystems;

public sealed class MechGunReloadSystem : EntitySystem
{
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private SharedContainerSystem _container = default!;
    [Dependency] private SharedPopupSystem _popup = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ADTMechComponent, MechReloadEvent>(OnReloadAction);
        SubscribeLocalEvent<ADTMechComponent, MechGunReloadMessage>(OnReloadMessage);

        SubscribeLocalEvent<MechMagazineComponent, MapInitEvent>(OnMagazineMapInit);
        SubscribeLocalEvent<MechMagazineComponent, ExaminedEvent>(OnMagazineExamined);
    }

    private void OnMagazineMapInit(Entity<MechMagazineComponent> ent, ref MapInitEvent args)
    {
        if (ent.Comp.Rounds >= 0)
            return;

        ent.Comp.Rounds = ent.Comp.Capacity;
        Dirty(ent);
    }

    private void OnMagazineExamined(Entity<MechMagazineComponent> ent, ref ExaminedEvent args)
    {
        args.PushMarkup(Loc.GetString("adt-mech-magazine-examine",
            ("rounds", GetRounds(ent.Comp)),
            ("capacity", ent.Comp.Capacity)));
    }

    private void OnReloadAction(Entity<ADTMechComponent> ent, ref MechReloadEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = true;

        var gun = CompOrNull<MechComponent>(ent)?.CurrentSelectedEquipment;
        if (!TryComp<BallisticMechAmmoProviderComponent>(gun, out var ammo))
        {
            _popup.PopupClient(Loc.GetString("adt-mech-reload-no-weapon"), ent, args.Performer);
            return;
        }

        TryReload(ent, (gun.Value, ammo), args.Performer);
    }

    private void OnReloadMessage(Entity<ADTMechComponent> ent, ref MechGunReloadMessage args)
    {
        var gun = GetEntity(args.Equipment);
        if (!TryComp<MechComponent>(ent, out var mech) || !mech.EquipmentContainer.Contains(gun))
            return;

        if (!TryComp<BallisticMechAmmoProviderComponent>(gun, out var ammo))
            return;

        TryReload(ent, (gun, ammo), args.Actor);
    }

    public int GetRounds(MechMagazineComponent magazine)
    {
        return magazine.Rounds < 0 ? magazine.Capacity : magazine.Rounds;
    }

    public bool IsReloading(BallisticMechAmmoProviderComponent ammo)
    {
        return ammo.PendingRounds > 0 && ammo.ReloadEnd > _timing.CurTime;
    }

    public void CompleteReload(Entity<BallisticMechAmmoProviderComponent> gun)
    {
        if (gun.Comp.PendingRounds <= 0 || gun.Comp.ReloadEnd > _timing.CurTime)
            return;

        gun.Comp.Shots = Math.Min(gun.Comp.Capacity, gun.Comp.Shots + gun.Comp.PendingRounds);
        gun.Comp.PendingRounds = 0;
        Dirty(gun);
    }

    public bool TryReload(EntityUid mech, Entity<BallisticMechAmmoProviderComponent> gun, EntityUid? user)
    {
        CompleteReload(gun);

        if (IsReloading(gun.Comp))
        {
            Popup("adt-mech-reload-busy", mech, user);
            return false;
        }

        var needed = gun.Comp.Capacity - gun.Comp.Shots;
        if (needed <= 0)
        {
            Popup("adt-mech-reload-full", mech, user);
            return false;
        }

        if (FindMagazine(mech, gun.Comp) is not { } magazine)
        {
            _audio.PlayPredicted(gun.Comp.NoAmmoForReload, mech, user);
            Popup("adt-mech-reload-no-magazine", mech, user);
            return false;
        }

        var rounds = GetRounds(magazine.Comp);
        var taken = Math.Min(needed, rounds);

        magazine.Comp.Rounds = rounds - taken;
        Dirty(magazine);

        if (magazine.Comp.Rounds <= 0)
            PredictedQueueDel(magazine.Owner);

        gun.Comp.PendingRounds = taken;
        gun.Comp.ReloadEnd = _timing.CurTime + TimeSpan.FromSeconds(gun.Comp.ReloadTime);
        Dirty(gun);

        _audio.PlayPredicted(gun.Comp.ReloadSound, mech, user);

        if (user != null)
            _popup.PopupClient(Loc.GetString("adt-mech-reload-started", ("item", gun.Owner)), mech, user.Value);

        return true;
    }

    private Entity<MechMagazineComponent>? FindMagazine(EntityUid mech, BallisticMechAmmoProviderComponent ammo)
    {
        if (!_container.TryGetContainer(mech, ammo.AmmoContainerId, out var container))
            return null;

        Entity<MechMagazineComponent>? best = null;
        foreach (var ent in container.ContainedEntities)
        {
            if (!TryComp<MechMagazineComponent>(ent, out var magazine) || magazine.MagazineType != ammo.AmmoType)
                continue;

            if (GetRounds(magazine) <= 0 || TerminatingOrDeleted(ent))
                continue;

            if (best == null || GetRounds(magazine) < GetRounds(best.Value.Comp))
                best = (ent, magazine);
        }

        return best;
    }

    private void Popup(string key, EntityUid mech, EntityUid? user)
    {
        if (user != null)
            _popup.PopupClient(Loc.GetString(key), mech, user.Value);
    }
}
