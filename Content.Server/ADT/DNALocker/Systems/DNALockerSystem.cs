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
using Content.Server.Administration.Logs;
using Content.Shared.Database;

namespace Content.Server.DNALocker;

public sealed partial class DNALockerSystem : EntitySystem
{
    [Dependency] private readonly SharedAudioSystem _audioSystem = default!;
    [Dependency] private readonly ExplosionSystem _explosion = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    [Dependency] private readonly IAdminLogManager _adminLogger = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<DNALockerComponent, GetVerbsEvent<AlternativeVerb>>(OnAltVerb);
        SubscribeLocalEvent<DNALockerComponent, GotEquippedEvent>(OnEquip);
        SubscribeLocalEvent<DNALockerComponent, GotEmaggedEvent>(OnGotEmagged);
    }

    public void LockEntity(EntityUid uid, DNALockerComponent component, EntityUid equipee)
    {
        if (!TryComp<DnaComponent>(equipee, out var dna))
        {
            ExplodeEntity(uid, component, equipee);
            return;
        }

        component.DNA = dna.DNA;
        _audioSystem.PlayPvs(component.LockSound, uid);
        var selfMessage = Loc.GetString("dna-locker-success");
        _popup.PopupEntity(selfMessage, equipee, equipee);
    }

    public void ExplodeEntity(EntityUid uid, DNALockerComponent component, EntityUid equipee)
    {
        if (!component.IsLocked)
            return;

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
        if (!component.Enabled)
            return;
        if (!component.IsLocked)
        {
            LockEntity(uid, component, args.Equipee);
            return;
        }

        if (TryComp<DnaComponent>(args.Equipee, out var dna))
        {
            if (component.DNA != null && component.DNA != dna.DNA)
            {
                _adminLogger.Add(LogType.AdminMessage, LogImpact.High, $"{ToPrettyString(args.Equipee)} exploded DNA Locker of {ToPrettyString(uid)}");
                ExplodeEntity(uid, component, args.Equipee);
            }
        }
    }

    private void OnGotEmagged(EntityUid uid, DNALockerComponent component, ref GotEmaggedEvent args)
    {
        if (!component.CanBeEmagged || !component.Enabled)
            return;

        component.DNA = string.Empty;
        _audioSystem.PlayPvs(component.EmagSound, uid);
        var userUid = args.UserUid;
        Timer.Spawn(1500, () =>
        {
            _audioSystem.PlayPvs(component.LockSound, uid);
            var selfMessage = Loc.GetString("dna-locker-unlock");
            _popup.PopupEntity(selfMessage, uid, userUid);
        });
        component.Enabled = !component.Enabled;
        args.Repeatable = true;
        args.Handled = true;
    }

    private void OnAltVerb(EntityUid uid, DNALockerComponent component, GetVerbsEvent<AlternativeVerb> args)
    {
        if (!component.IsLocked || !component.Enabled)
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
            component.DNA = string.Empty;
        }
        else
        {
            var denied = Loc.GetString("dna-locker-failure");
            _audioSystem.PlayPvs(component.DeniedSound, userUid);
            _popup.PopupEntity(denied, uid, userUid);
        }
    }
}
