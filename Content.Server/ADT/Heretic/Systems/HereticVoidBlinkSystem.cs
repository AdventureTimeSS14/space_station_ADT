using Content.Shared.ADT.Heretic;
using Content.Shared.ADT.Heretic.Components;
using Content.Shared.ADT.Heretic.Systems;
using Content.Shared.Damage;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Prototypes;
using Content.Shared.Damage.Systems;
using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Heretic;
using Content.Shared.Heretic.Components;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Popups;
using Content.Shared.Stunnable;
using Content.Shared.Verbs;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Server.ADT.Heretic.Systems;

public sealed partial class HereticVoidBlinkSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly DamageableSystem _damage = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly SharedHereticSystem _heretic = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedStunSystem _stun = default!;
    [Dependency] private readonly SharedTransformSystem _xform = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;

    private const string VoidPath = "Void";
    private static readonly ProtoId<DamageTypePrototype> ColdDamageType = "Cold";
    private static readonly SoundSpecifier BlinkSound = new SoundPathSpecifier("/Audio/ADT/Heretic/voidblink.ogg");

    private const float BlinkDamage = 15f;
    private const float KnockdownTime = 2f;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<HereticCombatMarkComponent, GetVerbsEvent<AlternativeVerb>>(OnAltVerbs);
        SubscribeNetworkEvent<HereticVoidBlinkRequestEvent>(OnBlinkRequest);
    }

    private void OnBlinkRequest(HereticVoidBlinkRequestEvent msg, EntitySessionEventArgs args)
    {
        if (args.SenderSession.AttachedEntity is not { } user)
            return;

        if (!CanBlink(user))
            return;

        var target = GetEntity(msg.Target);
        var coords = GetCoordinates(msg.Coordinates);

        BlinkToBestTarget(user, target, coords);
    }

    private void OnAltVerbs(Entity<HereticCombatMarkComponent> ent, ref GetVerbsEvent<AlternativeVerb> args)
    {
        if (!args.CanInteract || !args.CanAccess)
            return;

        if (ent.Comp.Path != VoidPath)
            return;

        var user = args.User;
        var target = args.Target;

        if (!CanBlink(user))
            return;

        args.Verbs.Add(new AlternativeVerb
        {
            Text = Loc.GetString("heretic-void-blink-verb"),
            Priority = 2,
            Act = () => BlinkTarget(user, target),
        });
    }

    public bool CanBlink(EntityUid user)
    {
        if (TerminatingOrDeleted(user))
            return false;

        if (!_heretic.TryGetHereticComponent(user, out var heretic, out _))
            return false;

        if (heretic.CurrentPath != VoidPath)
            return false;

        if (!TryComp<HandsComponent>(user, out var hands))
            return false;

        if (_hands.TryGetHeldItem((user, hands), hands.ActiveHandId, out var held))
        {
            if (!HasComp<HereticBladeComponent>(held.Value))
                return false;
        }

        return true;
    }

    public void Blink(EntityUid user, EntityUid target)
    {
        BlinkTarget(user, target);
    }

    public void BlinkToBestTarget(EntityUid user, EntityUid target, EntityCoordinates coords)
    {
        if (IsValidBlinkTarget(target))
        {
            BlinkTarget(user, target);
            return;
        }

        var userMapCoords = _xform.ToMapCoordinates(Transform(user).Coordinates);
        var searchMapCoords = coords.IsValid(EntityManager)
            ? _xform.ToMapCoordinates(coords)
            : userMapCoords;

        if (searchMapCoords.MapId != userMapCoords.MapId)
        {
            _popup.PopupEntity(Loc.GetString("heretic-void-blink-none"), user, user);
            return;
        }

        EntityUid? best = null;
        var bestDist = float.MaxValue;

        // First check in immediate vicinity of clicked coords (e.g. for people lying down)
        foreach (var entity in _lookup.GetEntitiesInRange(searchMapCoords, 3.0f))
        {
            if (!IsValidBlinkTarget(entity))
                continue;

            var dist = (_xform.ToMapCoordinates(Transform(entity).Coordinates).Position - searchMapCoords.Position).LengthSquared();
            if (dist < bestDist)
            {
                bestDist = dist;
                best = entity;
            }
        }

        // If not found right under cursor, search wider radius around click up to 16 tiles
        if (best == null)
        {
            bestDist = float.MaxValue;
            foreach (var entity in _lookup.GetEntitiesInRange(searchMapCoords, 16.0f))
            {
                if (!IsValidBlinkTarget(entity))
                    continue;

                var dist = (_xform.ToMapCoordinates(Transform(entity).Coordinates).Position - searchMapCoords.Position).LengthSquared();
                if (dist < bestDist)
                {
                    bestDist = dist;
                    best = entity;
                }
            }
        }

        if (best != null)
        {
            BlinkTarget(user, best.Value);
        }
        else
        {
            _popup.PopupEntity(Loc.GetString("heretic-void-blink-none"), user, user);
        }
    }

    public void BlinkTarget(EntityUid user, EntityUid target)
    {
        if (!IsValidBlinkTarget(target))
        {
            _popup.PopupEntity(Loc.GetString("heretic-void-blink-none"), user, user);
            return;
        }

        var dest = Transform(target).Coordinates;

        _audio.PlayPvs(BlinkSound, user);
        _xform.SetCoordinates(user, dest);
        _audio.PlayPvs(BlinkSound, user);

        if (TryComp<DamageableComponent>(target, out var damageable))
        {
            var spec = new DamageSpecifier();
            spec.DamageDict.Add(_proto.Index<DamageTypePrototype>(ColdDamageType), BlinkDamage);
            _damage.TryChangeDamage((target, damageable), spec, ignoreResistances: true, origin: user);
        }

        _stun.TryKnockdown((target, null), TimeSpan.FromSeconds(KnockdownTime), true);
    }

    private bool IsValidBlinkTarget(EntityUid uid)
    {
        if (!uid.IsValid() || TerminatingOrDeleted(uid))
            return false;

        if (!HasMark(uid))
            return false;

        if (IsImprisoned(uid))
            return false;

        if (!HasComp<MobStateComponent>(uid) || _mobState.IsDead(uid))
            return false;

        return true;
    }

    private bool HasMark(EntityUid uid)
    {
        return TryComp<HereticCombatMarkComponent>(uid, out var mark) && mark.Path == VoidPath;
    }

    private bool IsImprisoned(EntityUid uid)
    {
        return HasComp<VoidPrisonComponent>(uid);
    }
}
