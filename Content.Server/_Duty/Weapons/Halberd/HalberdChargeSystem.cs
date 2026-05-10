using System.Numerics;
using Content.Server.Chat.Systems;
using Content.Shared._Duty.Weapons.Halberd;
using Content.Shared.Actions;
using Content.Shared.Chat;
using Content.Shared.CombatMode;
using Content.Shared.Damage;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Systems;
using Content.Shared.FixedPoint;
using Content.Shared.Hands;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Physics;
using Content.Shared.Popups;
using Content.Shared.Stunnable;
using Content.Shared.Weapons.Melee;
using Content.Shared.Wieldable;
using Content.Shared.Wieldable.Components;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server._Duty.Weapons.Halberd;

public sealed class HalberdChargeSystem : EntitySystem
{
    [Dependency] private readonly SharedActionsSystem _actions = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly SharedStunSystem _stun = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;
    [Dependency] private readonly ChatSystem _chat = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<HalberdChargeComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<HalberdChargeComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<HalberdChargeActionEvent>(OnChargeAction);
        SubscribeLocalEvent<HalberdChargeComponent, ItemWieldedEvent>(OnWielded);
        SubscribeLocalEvent<HalberdChargeComponent, ItemUnwieldedEvent>(OnUnwielded);

        // Резист во время рывка — перехватываем урон до его применения
        SubscribeLocalEvent<HalberdChargeResistComponent, BeforeDamageChangedEvent>(OnBeforeDamage);

        // Не дропать алебарду при стане
        SubscribeLocalEvent<HalberdChargeComponent, KnockDownAttemptEvent>(OnKnockDownAttempt);

        // Алебарда не выпадает никогда кроме дизарма
        SubscribeLocalEvent<HalberdChargeComponent, DropAttemptEvent>(OnDropAttempt);
        SubscribeLocalEvent<HalberdChargeComponent, DisarmedEvent>(OnDisarmed);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<HalberdChargeComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if (!comp.IsCharging || comp.ChargeUser == null)
                continue;

            UpdateCharge(uid, comp);
        }
    }

    // ── Инициализация ─────────────────────────────────────────

    private void OnInit(EntityUid uid, HalberdChargeComponent comp, ComponentInit args)
    {
        // Action выдаётся только при wield — ничего не делаем здесь
    }

    private void OnShutdown(Entity<HalberdChargeComponent> ent, ref ComponentShutdown args)
    {
        _actions.RemoveAction(ent.Comp.ChargeActionEntity);
        ent.Comp.ChargeActionEntity = null;
    }

    private void OnWielded(EntityUid uid, HalberdChargeComponent comp, ItemWieldedEvent args)
    {
        // AddAction с существующим ChargeActionEntity просто привязывает его к юзеру
        // кулдаун в ActionComponent сохраняется автоматически
        _actions.AddAction(args.User, ref comp.ChargeActionEntity, comp.ChargeActionId);
    }

    private void OnUnwielded(EntityUid uid, HalberdChargeComponent comp, ItemUnwieldedEvent args)
    {
        // Убираем action при unwield/выброске
        _actions.RemoveAction(comp.ChargeActionEntity);
        comp.ChargeActionEntity = null;

        // Прерываем рывок если он шёл
        if (comp.IsCharging)
            StopCharge(uid, comp, ChargeEndReason.Miss);
    }

    // ── Резист ────────────────────────────────────────────────

    private void OnBeforeDamage(Entity<HalberdChargeResistComponent> ent, ref BeforeDamageChangedEvent args)
    {
        var mult = (FixedPoint2) (1f - ent.Comp.Resistance);
        args.Damage *= mult;
    }

    // ── Активация рывка ───────────────────────────────────────

    private void OnChargeAction(HalberdChargeActionEvent args)
    {
        if (args.Handled)
            return;

        var user = args.Performer;

        // Ищем алебарду с HalberdChargeComponent в руках
        EntityUid? halberdUid = null;
        HalberdChargeComponent? comp = null;
        foreach (var held in _hands.EnumerateHeld(user))
        {
            if (TryComp<HalberdChargeComponent>(held, out var c))
            {
                halberdUid = held;
                comp = c;
                break;
            }
        }

        if (halberdUid == null || comp == null)
            return;

        // Алебарда должна быть wielded
        if (!IsHalberdWielded(user, halberdUid.Value))
        {
            _popup.PopupEntity(Loc.GetString("halberd-charge-need-wield"), user, user, PopupType.SmallCaution);
            return;
        }

        if (comp.IsCharging)
            return;

        // Запрещаем рывок если игрок лежит (в стане)
        if (HasComp<KnockedDownComponent>(user))
        {
            _popup.PopupEntity(Loc.GetString("halberd-charge-need-stand"), user, user, PopupType.SmallCaution);
            return;
        }

        // Направление к курсору
        var userPos = _transform.GetWorldPosition(user);
        var targetPos = args.Target.ToMapPos(EntityManager, _transform);
        var direction = targetPos - userPos;

        if (direction.LengthSquared() < 0.001f)
            direction = new Vector2(1f, 0f);
        else
            direction = Vector2.Normalize(direction);

        // Запускаем рывок
        comp.IsCharging = true;
        comp.ChargeUser = user;
        comp.ChargeDirection = direction;
        comp.ChargeStartPos = userPos;

        // Звук и крик старта
        _audio.PlayPvs(new SoundPathSpecifier("/Audio/Voice/Human/malescream_2.ogg"), user);
        _chat.TrySendInGameICMessage(user, "АААААА!!!", InGameICChatType.Speak, false);

        // Резист
        var resist = EnsureComp<HalberdChargeResistComponent>(user);
        resist.Resistance = comp.ChargeResistance;

        args.Handled = true;
    }

    // ── Тик рывка ─────────────────────────────────────────────

    private void UpdateCharge(EntityUid halberdUid, HalberdChargeComponent comp)
    {
        var user = comp.ChargeUser!.Value;

        if (!Exists(user) || !TryComp<PhysicsComponent>(user, out var physics))
        {
            StopCharge(halberdUid, comp, ChargeEndReason.Miss);
            return;
        }

        var userPos = _transform.GetWorldPosition(user);
        var traveled = Vector2.Distance(userPos, comp.ChargeStartPos);

        // Плавный рывок: разгон на первых 2 тайлах, торможение на последних 2
        float speedMult;
        var rampDown = comp.ChargeDistance - 2f;
        if (traveled < 2f)
            speedMult = Math.Clamp(traveled / 2f, 0.2f, 1f);
        else if (traveled > rampDown)
            speedMult = Math.Clamp((comp.ChargeDistance - traveled) / 2f, 0.2f, 1f);
        else
            speedMult = 1f;

        _physics.SetLinearVelocity(user, comp.ChargeDirection * comp.ChargeSpeed * speedMult, body: physics);

        // Дистанция исчерпана
        if (traveled >= comp.ChargeDistance)
        {
            StopCharge(halberdUid, comp, ChargeEndReason.Miss);
            return;
        }

        // Проверяем коллизии — только Hard тела (жидкости, декали игнорируются)
        var box = new Box2(userPos - new Vector2(0.45f, 0.45f), userPos + new Vector2(0.45f, 0.45f));
        var intersecting = new HashSet<EntityUid>();
        _lookup.GetEntitiesIntersecting(Transform(user).MapID, box, intersecting, LookupFlags.Dynamic | LookupFlags.Static);

        foreach (var target in intersecting)
        {
            if (target == user || target == halberdUid)
                continue;

            if (!TryComp<PhysicsComponent>(target, out var targetPhysics))
                continue;

            // Игнорируем всё что не Hard (кровь, лужи, декали)
            if (!targetPhysics.Hard)
                continue;

            // Стена — статичный объект с DamageableComponent (лужи/декали его не имеют)
            if (targetPhysics.BodyType == BodyType.Static && HasComp<DamageableComponent>(target))
            {
                StopCharge(halberdUid, comp, ChargeEndReason.Wall);
                return;
            }

            // Моб — динамическое Hard тело с DamageableComponent
            if (HasComp<DamageableComponent>(target))
            {
                HitEntity(halberdUid, comp, target);
                StopCharge(halberdUid, comp, ChargeEndReason.HitEntity);
                return;
            }
        }
    }

    // ── Попадание в сущность ──────────────────────────────────

    private void HitEntity(EntityUid halberdUid, HalberdChargeComponent comp, EntityUid target)
    {
        var user = comp.ChargeUser!.Value;

        var damage = new DamageSpecifier();
        damage.DamageDict["Slash"] = (FixedPoint2) comp.ChargeDamage;
        _damageable.TryChangeDamage(target, damage, origin: user);

        _audio.PlayPvs(new SoundPathSpecifier("/Audio/Weapons/slash.ogg"), user);
        _chat.TrySendInGameICMessage(user, "НААА!!!", InGameICChatType.Speak, false);
    }

    // ── Завершение рывка ──────────────────────────────────────

    private void StopCharge(EntityUid halberdUid, HalberdChargeComponent comp, ChargeEndReason reason)
    {
        var user = comp.ChargeUser!.Value;

        comp.IsCharging = false;
        comp.ChargeUser = null;

        // Останавливаем физику
        if (TryComp<PhysicsComponent>(user, out var physics))
            _physics.SetLinearVelocity(user, Vector2.Zero, body: physics);

        // Убираем резист
        RemCompDeferred<HalberdChargeResistComponent>(user);

        // Звуки по ситуации
        switch (reason)
        {
            case ChargeEndReason.Wall:
                _audio.PlayPvs(new SoundPathSpecifier("/Audio/Effects/metal_slam1.ogg"), user);
                var cry = _random.Pick(new[] { "СУКА!!", "БЛЯТЬ!!!" });
                _chat.TrySendInGameICMessage(user, cry, InGameICChatType.Speak, false);
                break;
        }

        // Стан 2 сек во всех случаях
        _stun.TryKnockdown(user, TimeSpan.FromSeconds(2f), true);

        // Кулдаун на action (кулдаун хранится в ActionComponent и не теряется при unwield/wield)
        if (comp.ChargeActionEntity.HasValue)
            _actions.StartUseDelay(comp.ChargeActionEntity.Value);
    }

    // ── Не дропать алебарду при стане ────────────────────────

    private void OnDropAttempt(EntityUid uid, HalberdChargeComponent comp, DropAttemptEvent args)
    {
        // Запрещаем любой дроп — дизарм обрабатывается отдельно
        args.Cancel();
    }

    private void OnDisarmed(EntityUid uid, HalberdChargeComponent comp, ref DisarmedEvent args)
    {
        // При дизарме — принудительно выбрасываем алебарду из рук
        // Но только если дизарм успешный (есть IsStunned)
        if (!args.IsStunned)
            return;

        if (_hands.IsHolding(args.Target, uid, out var hand))
            _hands.TryDrop(args.Target, uid, checkActionBlocker: false);
    }

    // ── Не дропать алебарду при стане ────────────────────────

    private void OnKnockDownAttempt(EntityUid uid, HalberdChargeComponent comp, ref KnockDownAttemptEvent args)
    {
        // Пока алебарда в руках (есть пользователь) — не дропать
        if (comp.ChargeUser.HasValue)
            args.Drop = false;
    }

    // ── Хелперы ───────────────────────────────────────────────

    private bool IsHalberdWielded(EntityUid user, EntityUid halberd)
    {
        if (!_hands.IsHolding(user, halberd, out _))
            return false;

        if (!TryComp<WieldableComponent>(halberd, out var wieldable))
            return true;

        return wieldable.Wielded;
    }
}

public enum ChargeEndReason
{
    HitEntity,
    Wall,
    Miss,
}
