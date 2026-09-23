//

using System.Linq;
using Content.Shared.Access;
using Content.Shared.Access.Components;
using Content.Shared.Access.Systems;
using Content.Shared.DoAfter;
using Content.Shared.Examine;
using Content.Shared.Heretic.Components.PathSpecific.Lock;
using Content.Shared.ADT.Heretic.Systems;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Content.Shared.Verbs;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;

namespace Content.Shared.ADT.Heretic.Systems.PathSpecific.Lock;

public abstract partial class SharedEldritchIdCardSystem : EntitySystem
{
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly SharedIdCardSystem _idCard = default!;
    [Dependency] private readonly SharedAccessSystem _access = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedHereticSystem _heretic = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly LockPortalSystem _portal = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<EldritchIdCardComponent, ExaminedEvent>(OnExamine);
        SubscribeLocalEvent<EldritchIdCardComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<EldritchIdCardComponent, GetVerbsEvent<AlternativeVerb>>(OnAltVerb);
        SubscribeLocalEvent<EldritchIdCardComponent, BeforeRangedInteractEvent>(OnBeforeInteract);
        SubscribeLocalEvent<EldritchIdCardComponent, LockPortalDoAfterEvent>(OnDoAfter);
    }

    private void OnDoAfter(Entity<EldritchIdCardComponent> ent, ref LockPortalDoAfterEvent args)
    {
        if (args.Cancelled || args.Handled)
            return;

        args.Handled = true;

        if (args.Target is not { } target)
            return;

        if (_portal.IsDoorOccupied(target, args.User))
            return;

        if (_net.IsClient)
            return;

        var portalOneResolved = Exists(ent.Comp.PortalOne);
        var portalTwoResolved = Exists(ent.Comp.PortalTwo);

        if (portalOneResolved && portalTwoResolved)
        {
            QueueDel(ent.Comp.PortalOne);
            var newPortal = SpawnAttachedTo(ent.Comp.Portal, Transform(target).Coordinates);
            _transform.SetParent(newPortal, target);
            var newPortalComp = EnsureComp<LockPortalComponent>(newPortal);
            var portalTwoComp = EnsureComp<LockPortalComponent>(ent.Comp.PortalTwo!.Value);
            newPortalComp.Inverted = ent.Comp.Inverted;
            portalTwoComp.Inverted = ent.Comp.Inverted;
            newPortalComp.LinkedPortal = ent.Comp.PortalTwo.Value;
            portalTwoComp.LinkedPortal = newPortal;
            Dirty(newPortal, newPortalComp);
            Dirty(ent.Comp.PortalTwo.Value, portalTwoComp);
            ent.Comp.PortalOne = ent.Comp.PortalTwo.Value;
            ent.Comp.PortalTwo = newPortal;
            _popup.PopupEntity(Loc.GetString("eldritch-id-card-component-link-two"), args.User, args.User);
            return;
        }

        if (!portalOneResolved)
        {
            var newPortal = SpawnAttachedTo(ent.Comp.Portal, Transform(target).Coordinates);
            _transform.SetParent(newPortal, target);
            ent.Comp.PortalOne = newPortal;
            var newPortalComp = EnsureComp<LockPortalComponent>(newPortal);
            newPortalComp.Inverted = ent.Comp.Inverted;
            Dirty(newPortal, newPortalComp);

            if (!portalTwoResolved)
            {
                _popup.PopupEntity(Loc.GetString("eldritch-id-card-component-link-one"), args.User, args.User);
                return;
            }

            _popup.PopupEntity(Loc.GetString("eldritch-id-card-component-link-two"), args.User, args.User);

            var portalTwoComp = EnsureComp<LockPortalComponent>(ent.Comp.PortalTwo!.Value);
            portalTwoComp.Inverted = ent.Comp.Inverted;
            Dirty(ent.Comp.PortalTwo.Value, portalTwoComp);
            newPortalComp.LinkedPortal = ent.Comp.PortalTwo.Value;
            portalTwoComp.LinkedPortal = newPortal;
            return;
        }

        if (!portalTwoResolved)
        {
            var newPortal = SpawnAttachedTo(ent.Comp.Portal, Transform(target).Coordinates);
            _transform.SetParent(newPortal, target);
            ent.Comp.PortalTwo = newPortal;
            var newPortalComp = EnsureComp<LockPortalComponent>(newPortal);
            newPortalComp.Inverted = ent.Comp.Inverted;
            Dirty(newPortal, newPortalComp);

            if (!portalOneResolved)
            {
                _popup.PopupEntity(Loc.GetString("eldritch-id-card-component-link-one"), args.User, args.User);
                return;
            }

            _popup.PopupEntity(Loc.GetString("eldritch-id-card-component-link-two"), args.User, args.User);

            var portalOneComp = EnsureComp<LockPortalComponent>(ent.Comp.PortalOne!.Value);
            portalOneComp.Inverted = ent.Comp.Inverted;
            Dirty(ent.Comp.PortalOne.Value, portalOneComp);
            newPortalComp.LinkedPortal = ent.Comp.PortalOne.Value;
            portalOneComp.LinkedPortal = newPortal;
        }
    }

    private void OnBeforeInteract(Entity<EldritchIdCardComponent> ent, ref BeforeRangedInteractEvent args)
    {
        if (!args.CanReach || args.Target == null || !_heretic.IsHereticOrGhoul(args.User))
            return;

        var target = args.Target.Value;

        if (_idCard.TryFindIdCard(target, out var victimCard) && victimCard.Owner != args.User)
        {
            args.Handled = true;
            EatCard(ent, victimCard, args.User);
            return;
        }

        if (TryComp(target, out LockPortalComponent? portal))
        {
            args.Handled = true;
            InvertPortals((target, portal), args.User);
            return;
        }

        if (!_portal.IsDoorValid(target))
            return;

        var doArgs = new DoAfterArgs(EntityManager,
            args.User,
            ent.Comp.PortalCreationTime,
            new LockPortalDoAfterEvent(),
            ent,
            target,
            ent)
        {
            BreakOnMove = true,
            BreakOnDamage = true,
            NeedHand = true,
            BreakOnWeightlessMove = false,
        };

        if (_doAfter.TryStartDoAfter(doArgs))
            args.Handled = true;
    }

    private void OnAltVerb(Entity<EldritchIdCardComponent> ent, ref GetVerbsEvent<AlternativeVerb> args)
    {
        if (!_heretic.IsHereticOrGhoul(args.User))
            return;

        var user = args.User;

        args.Verbs.Add(new()
        {
            Act = () => Invert(ent, user),
            Text = Loc.GetString("eldritch-id-card-component-invert"),
            Message = Loc.GetString("eldritch-id-card-component-invert-message"),
            Icon = new SpriteSpecifier.Rsi(new("Objects/Misc/id_cards.rsi"), "gold"),
            Priority = 2
        });
    }

    private void OnExamine(Entity<EldritchIdCardComponent> ent, ref ExaminedEvent args)
    {
        if (!_heretic.IsHereticOrGhoul(args.Examiner))
            return;

        if (ent.Comp.Inverted)
            args.PushMarkup(Loc.GetString("eldritch-id-card-component-examine-inverted"));

        args.PushMarkup(Loc.GetString("eldritch-id-card-component-examine-message"));
    }

    private void Invert(Entity<EldritchIdCardComponent> ent, EntityUid user)
    {
        ent.Comp.Inverted = !ent.Comp.Inverted;
        DirtyField(ent.Owner, ent.Comp, nameof(EldritchIdCardComponent.Inverted));

        _popup.PopupEntity(Loc.GetString("eldritch-id-card-component-on-invert", ("inverted", ent.Comp.Inverted)),
            user,
            user);
    }

    private void InvertPortals(Entity<LockPortalComponent> ent, EntityUid user)
    {
        ent.Comp.Inverted = !ent.Comp.Inverted;
        DirtyField(ent.AsNullable(), nameof(LockPortalComponent.Inverted));

        _popup.PopupEntity(Loc.GetString("eldritch-id-card-component-portal-inverted",
                ("inverted", ent.Comp.Inverted)),
            user,
            user);

        if (!Exists(ent.Comp.LinkedPortal) || !TryComp(ent.Comp.LinkedPortal.Value, out LockPortalComponent? portal2))
            return;

        portal2.Inverted = ent.Comp.Inverted;
        DirtyField(ent.Comp.LinkedPortal.Value, portal2, nameof(LockPortalComponent.Inverted));
    }

    private void EatCard(Entity<EldritchIdCardComponent> ent, Entity<IdCardComponent> idCard, EntityUid user)
    {
        if (ent.Owner == idCard.Owner)
            return;

        if (TryComp(idCard, out AccessComponent? access))
        {
            var existing = _access.TryGetTags(ent) ?? [];
            var merged = new HashSet<ProtoId<AccessLevelPrototype>>(existing);
            merged.UnionWith(access.Tags);
            _access.TrySetTags(ent, merged);
        }

        _audio.PlayPredicted(ent.Comp.EatSound, Transform(idCard.Owner).Coordinates, user);
        PredictedDel(idCard.Owner);
    }

    protected virtual void OnMapInit(Entity<EldritchIdCardComponent> ent, ref MapInitEvent args)
    {
        if (!TryComp(ent, out IdCardComponent? id))
            return;

        id.BypassLogging = true;
        ent.Comp.CurrentProto = Prototype(ent)?.ID;
        Dirty(ent);
    }
}

[Serializable, NetSerializable]
public sealed partial class LockPortalDoAfterEvent : SimpleDoAfterEvent;
