using Content.Server.Forensics;
using Content.Shared.Inventory;
using Content.Shared.Inventory.Events;
using Content.Server.Explosion.EntitySystems;
using Robust.Shared.Audio.Systems;
using Content.Shared.Popups;
using Content.Shared.Emag.Systems;
using Robust.Shared.Timing;
using Content.Shared.Interaction.Components;
using Content.Shared.Verbs;
using Robust.Shared.Utility;

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

        SubscribeLocalEvent<DNALockerComponent, GetVerbsEvent<AlternativeVerb>>(OnAltVerb);
        SubscribeLocalEvent<DNALockerComponent, GotEquippedEvent>(OnEquip);
        SubscribeLocalEvent<DNALockerComponent, GotEmaggedEvent>(OnGotEmagged);
    }

    public void LockEntity(EntityUid uid, DNALockerComponent component, EntityUid equipee)
    {
        var dna = EnsureComp<DnaComponent>(equipee);
        component.DNA = dna.DNA;
        component.Locked = true;
        _audioSystem.PlayPvs(component.LockSound, uid);
        var selfMessage = Loc.GetString("dna-locker-success");
        _popup.PopupEntity(selfMessage, equipee, equipee);
    }

    public void UnlockEntity(EntityUid uid, DNALockerComponent component, EntityUid equipee)
    {
        EnsureComp<UnremoveableComponent>(uid);
        var selfMessage = Loc.GetString("dna-locker-failure");
        var unremoveableMessage = Loc.GetString("dna-locker-unremoveable");

        _popup.PopupEntity(unremoveableMessage, equipee, equipee, PopupType.LargeCaution);
        _audioSystem.PlayPvs(component.LockerExplodeSound, uid);
        Timer.Spawn(3000, () =>
        {
            _popup.PopupEntity(selfMessage, equipee, equipee, PopupType.LargeCaution);
            _explosion.QueueExplosion(equipee, "Default", 200f, 10f, 100f, 1f);
            QueueDel(uid);
        });
    }

    private void OnEquip(EntityUid uid, DNALockerComponent component, GotEquippedEvent args)
    {
        if (!component.Locked)
        {
            LockEntity(uid, component, args.Equipee);
            return;
        }

        var dna = EnsureComp<DnaComponent>(args.Equipee);
        if (component.DNA != null && component.DNA != dna.DNA)
        {
            UnlockEntity(uid, component, args.Equipee);
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

    private void OnAltVerb(EntityUid uid, DNALockerComponent component, GetVerbsEvent<AlternativeVerb> args)
    {
        if (!component.Locked)
            return;

        AlternativeVerb verbDNALock = new()
        {
            Act = () => MakeUnlocked(uid, component, args.User),
            Text = Loc.GetString("dna-locker-verb-name"),
            Icon = new SpriteSpecifier.Texture(new("/Textures/Interface/VerbIcons/fold.svg.192dpi.png")),
        };
        args.Verbs.Add(verbDNALock);
    }

    private void MakeUnlocked(EntityUid uid, DNALockerComponent component, EntityUid userUid)
    {
        if (TryComp<DnaComponent>(userUid, out var userDNAComponent) && component.DNA == userDNAComponent.DNA)
        {
            var unlocked = Loc.GetString("dna-locker-unlock");
            _audioSystem.PlayPvs(component.LockSound, userUid);
            _popup.PopupEntity(unlocked, uid, userUid);
            component.Locked = false;
        }
        else
        {
            var denied = Loc.GetString("dna-locker-failure");
            _audioSystem.PlayPvs(component.DeniedSound, userUid);
            _popup.PopupEntity(denied, uid, userUid);
        }
    }
}
