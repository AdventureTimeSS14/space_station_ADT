using Content.Server.Polymorph.Components;
using Content.Server.Polymorph.Systems;
using Content.Shared.ADT.Drake;
using Content.Shared.ADT.Drake.Loot;
using Content.Shared.Administration.Systems;
using Content.Shared.Chat;
using Content.Shared.CombatMode;
using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Content.Shared.Damage.Systems;
using Content.Shared.DoAfter;
using Content.Shared.FixedPoint;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Popups;
using Robust.Shared.Map;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Server.ADT.Drake.Loot;

public sealed class ADTLesserDrakeSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly ADTDrakeAttacksSystem _attacks = default!;
    [Dependency] private readonly ADTDrakeSwoopSystem _swoop = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly MobThresholdSystem _thresholds = default!;
    [Dependency] private readonly PolymorphSystem _polymorph = default!;
    [Dependency] private readonly RejuvenateSystem _rejuvenate = default!;
    [Dependency] private readonly SharedCombatModeSystem _combatMode = default!;
    [Dependency] private readonly SharedChatSystem _chat = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    private readonly HashSet<Entity<MobStateComponent>> _mobs = new();

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ADTDragonFormActionEvent>(OnDragonForm);
        SubscribeLocalEvent<ADTDragonFormDoAfterEvent>(OnDragonFormDoAfter);

        SubscribeLocalEvent<ADTDrakeComponent, ADTLesserDrakeFireConeActionEvent>(OnFireCone);
        SubscribeLocalEvent<ADTDrakeComponent, ADTLesserDrakeSwoopActionEvent>(OnSwoop);
    }

    private void OnDragonForm(ADTDragonFormActionEvent args)
    {
        if (args.Handled)
            return;

        var user = args.Performer;

        if (TryComp<PolymorphedEntityComponent>(user, out var polymorphed))
        {
            if (!HasComp<ADTLesserDrakeComponent>(user))
            {
                _popup.PopupEntity(Loc.GetString("adt-dragon-form-wrong-form"), user, user);
                return;
            }

            args.Handled = true;
            Invoke(user, args);

            if (_swoop.IsSwooping(user))
                return;

            Unshapeshift((user, polymorphed), args.ConvertGroup);
            return;
        }

        args.Handled = true;
        Invoke(user, args);

        _popup.PopupEntity(Loc.GetString("adt-dragon-form-begin-others", ("user", user)), user, Filter.PvsExcept(user), true, PopupType.MediumCaution);
        _popup.PopupEntity(Loc.GetString("adt-dragon-form-begin-self"), user, user, PopupType.MediumCaution);

        var doAfter = new DoAfterArgs(EntityManager,
            user,
            args.Delay,
            new ADTDragonFormDoAfterEvent(args.Polymorph, args.ConvertGroup),
            user)
        {
            BreakOnMove = true,
            NeedHand = false,
            Broadcast = true,
        };

        _doAfter.TryStartDoAfter(doAfter);
    }

    private void OnDragonFormDoAfter(ADTDragonFormDoAfterEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = true;

        var user = args.User;

        if (args.Cancelled)
        {
            _popup.PopupEntity(Loc.GetString("adt-dragon-form-fail"), user, user, PopupType.MediumCaution);
            return;
        }

        if (HasComp<PolymorphedEntityComponent>(user))
            return;

        var fraction = GetDamageFraction(user);

        if (_polymorph.PolymorphEntity(user, args.Polymorph) is not { } shape)
            return;

        ApplyDamageFraction(shape, fraction, args.ConvertGroup);
    }

    private void Unshapeshift(Entity<PolymorphedEntityComponent> shape, ProtoId<DamageGroupPrototype> convertGroup)
    {
        var fraction = GetDamageFraction(shape);

        if (_polymorph.Revert(shape.AsNullable()) is not { } caster)
            return;

        _rejuvenate.PerformRejuvenate(caster);
        ApplyDamageFraction(caster, fraction, convertGroup);
    }

    private void Invoke(EntityUid user, ADTDragonFormActionEvent args)
    {
        if (args.Emote is { } emote)
            _chat.TryEmoteWithChat(user, emote, ignoreActionBlocker: true);
    }

    private float GetDamageFraction(EntityUid uid)
    {
        if (!_thresholds.TryGetIncapThreshold(uid, out var max) || max.Value <= FixedPoint2.Zero)
            return 0f;

        return (float) (_damageable.GetTotalDamage(uid) / max.Value);
    }

    private void ApplyDamageFraction(EntityUid uid, float fraction, ProtoId<DamageGroupPrototype> convertGroup)
    {
        if (fraction <= 0f)
            return;

        if (!_thresholds.TryGetIncapThreshold(uid, out var max))
            return;

        if (!_prototype.TryIndex(convertGroup, out var group))
            return;

        var damage = new DamageSpecifier(group, max.Value * fraction);
        _damageable.TryChangeDamage(uid, damage, true, false);
    }

    private void OnFireCone(Entity<ADTDrakeComponent> ent, ref ADTLesserDrakeFireConeActionEvent args)
    {
        if (args.Handled || _mobState.IsDead(ent) || _swoop.IsSwooping(ent))
            return;

        if (!_combatMode.IsInCombatMode(ent))
            return;

        var now = _timing.CurTime;
        if (now < ent.Comp.NextRangedAt)
            return;

        args.Handled = true;

        ent.Comp.NextRangedAt = now + ent.Comp.RangedCooldown;
        _attacks.FireConeAt(ent, _transform.ToMapCoordinates(args.Target));
    }

    private void OnSwoop(Entity<ADTDrakeComponent> ent, ref ADTLesserDrakeSwoopActionEvent args)
    {
        if (args.Handled || _mobState.IsDead(ent) || _swoop.IsSwooping(ent))
            return;

        if (!_combatMode.IsInCombatMode(ent))
            return;

        var target = FindMob(ent, args.Target) ?? Spawn(args.TargetMarker, args.Target);
        ent.Comp.ManualTarget = target;

        if (!_swoop.TrySwoop(ent, target, false, args.Recovery, ADTDrakeSwoopFollowUp.LavaPools))
            return;

        args.Handled = true;
    }

    private EntityUid? FindMob(EntityUid drake, EntityCoordinates coords)
    {
        _mobs.Clear();
        _lookup.GetEntitiesInRange(_transform.ToMapCoordinates(coords), 0.5f, _mobs);

        foreach (var mob in _mobs)
        {
            if (mob.Owner != drake)
                return mob.Owner;
        }

        return null;
    }
}
