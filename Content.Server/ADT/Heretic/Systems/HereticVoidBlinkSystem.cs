using Content.Shared.ADT.Heretic.Components;
using Content.Shared.Heretic;
using Content.Shared.ADT.Heretic.Systems;
using Content.Shared.Damage;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Prototypes;
using Content.Shared.Damage.Systems;
using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Heretic.Components;
using Content.Shared.Verbs;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Popups;
using Content.Shared.Stunnable;
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

    private const string VoidPath = "Void";
    private static readonly ProtoId<DamageTypePrototype> ColdDamageType = "Cold";

    private const float BlinkDamage = 15f;
    private const float KnockdownTime = 2f;
    private const float StopDistance = 1.5f;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<HereticCombatMarkComponent, GetVerbsEvent<AlternativeVerb>>(OnAltVerbs);
    }

    private void OnAltVerbs(Entity<HereticCombatMarkComponent> ent, ref GetVerbsEvent<AlternativeVerb> args)
    {
        if (!args.CanInteract || !args.CanAccess)
            return;

        if (ent.Comp.Path != VoidPath)
            return;

        var user = args.User;
        var target = args.Target;

        if (!_heretic.TryGetHereticComponent(user, out _, out _))
            return;

        var hands = args.Hands;
        if (hands == null || !_hands.TryGetHeldItem((user, hands), hands.ActiveHandId, out var held))
            return;

        if (!HasComp<HereticBladeComponent>(held.Value))
            return;

        args.Verbs.Add(new AlternativeVerb
        {
            Text = Loc.GetString("heretic-void-blink-verb"),
            Priority = 2,
            Act = () => Blink(user, target),
        });
    }

    private void Blink(EntityUid user, EntityUid target)
    {
        var marked = new List<EntityUid>();
        var query = AllEntityQuery<HereticCombatMarkComponent>();
        while (query.MoveNext(out var uid, out var mark))
        {
            if (mark.Path != VoidPath)
                continue;

            if (IsImprisoned(uid))
                continue;

            if (!HasComp<MobStateComponent>(uid) || _mobState.IsDead(uid))
                continue;

            marked.Add(uid);
        }

        if (marked.Count == 0)
        {
            _popup.PopupEntity(Loc.GetString("heretic-void-blink-none"), user, user);
            return;
        }

        foreach (var uid in marked)
        {
            var dest = Transform(uid).Coordinates;
            var dir = (Transform(user).Coordinates.Position - dest.Position).Normalized();
            if (dir.LengthSquared() < 0.01f)
                dir = new System.Numerics.Vector2(1f, 0f);

            _xform.SetCoordinates(user, dest.Offset(-dir * StopDistance));

            if (TryComp<DamageableComponent>(uid, out var damageable))
            {
                var spec = new DamageSpecifier();
                spec.DamageDict.Add(_proto.Index<DamageTypePrototype>(ColdDamageType), BlinkDamage);
                _damage.TryChangeDamage((uid, damageable), spec, ignoreResistances: true, origin: user);
            }

            _stun.TryKnockdown((uid, null), TimeSpan.FromSeconds(KnockdownTime), true);
        }
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
