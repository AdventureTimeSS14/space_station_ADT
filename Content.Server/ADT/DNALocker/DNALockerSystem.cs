using Content.Server.Forensics;
using Content.Shared.Inventory;
using Content.Shared.Inventory.Events;
using Content.Server.Explosion.EntitySystems;
using Robust.Shared.Audio.Systems;
using Content.Shared.Popups;
using Content.Shared.Emag.Systems;
using Robust.Shared.Timing;
using Content.Shared.Interaction.Components;

namespace Content.Server.DNALocker;

public sealed partial class DNALockerSystem : EntitySystem
{
    [Dependency] private readonly SharedAudioSystem _audioSystem = default!;
    [Dependency] private readonly ExplosionSystem _explosion = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly InventorySystem _inventorySystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<DNALockerComponent, GotEquippedEvent>(OnEquip);
        SubscribeLocalEvent<DNALockerComponent, GotEmaggedEvent>(OnGotEmagged);
    }

    private void OnEquip(EntityUid uid, DNALockerComponent component, GotEquippedEvent args)
    {
        if (component.Locked == false)
        {
            var dna = EnsureComp<DnaComponent>(args.Equipee);
            component.DNA = dna.DNA;
            component.Locked = true;
            _audioSystem.PlayPvs(component.LockSound, uid);
            var selfMessage = Loc.GetString("dna-locker-success");
            _popup.PopupEntity(selfMessage, args.Equipee, args.Equipee);
        }

        if (component.Locked == true)
        {
            var dna = EnsureComp<DnaComponent>(args.Equipee);

            if (component.DNA != null && component.DNA != dna.DNA)
            {
                if (!HasComp<UnremoveableComponent>(uid))
                {
                    AddComp<UnremoveableComponent>(uid);
                }
                var selfMessage = Loc.GetString("dna-locker-explode");
                var unremoveableMessage = Loc.GetString("dna-locker-unremoveable");

                _popup.PopupEntity(unremoveableMessage, args.Equipee, args.Equipee, PopupType.LargeCaution);
                _audioSystem.PlayPvs(component.LockerExplodeSound, uid);
                Timer.Spawn(3000, () =>
                {
                    _popup.PopupEntity(selfMessage, args.Equipee, args.Equipee, PopupType.LargeCaution);
                    _explosion.QueueExplosion(args.Equipee, "Default", 200f, 10f, 100f, 1f);
                    QueueDel(uid);
                });
            }
        }
    }

    private void OnGotEmagged(EntityUid uid, DNALockerComponent component, ref GotEmaggedEvent args)
    {
        var ifEquipped = Loc.GetString("dna-locker-equipped");
        if (!(_inventorySystem.TryGetSlotEntity(args.UserUid, "outerClothing", out var slotItem) && slotItem == uid))
        {
            component.Locked = false;
            _audioSystem.PlayPvs(component.EmagSound, uid);
            var userUid = args.UserUid;
            Timer.Spawn(1500, () =>
            {
                _audioSystem.PlayPvs(component.LockSound, uid);
                var selfMessage = Loc.GetString("dna-locker-unlock");
                _popup.PopupEntity(selfMessage, uid, userUid);
            });
            args.Repeatable = true;
            args.Handled = true;
        }
        else
        {
            _audioSystem.PlayPvs(component.EmagSound, uid);
            _popup.PopupEntity(ifEquipped, uid, args.UserUid);
        }
    }
}