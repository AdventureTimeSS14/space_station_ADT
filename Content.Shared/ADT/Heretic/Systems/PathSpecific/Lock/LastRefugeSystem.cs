//

using Content.Shared.Actions;
using Content.Shared.ADT.Movement;
using Content.Shared.Damage;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Systems;
using Content.Shared.FixedPoint;
using Content.Shared.Heretic.Components.PathSpecific.Lock;
using Content.Shared.Humanoid;
using Content.Shared.Interaction.Events;
using Content.Shared.Magic.Events;
using Content.Shared.Mobs.Systems;
using Content.Shared.Movement.Systems;
using Content.Shared.Popups;
using Content.Shared.Projectiles;
using Content.Shared.StatusEffectNew;
using Content.Shared.Stealth;
using Content.Shared.Stealth.Components;
using Content.Shared.Strip.Components;
using Content.Shared.Throwing;
using Content.Shared.Weapons.Ranged.Events;
using Robust.Shared.Physics.Events;

namespace Content.Shared.ADT.Heretic.Systems.PathSpecific.Lock;

public sealed partial class LastRefugeSystem : EntitySystem
{
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly MovementSpeedModifierSystem _movement = default!;
    [Dependency] private readonly SharedActionsSystem _actions = default!;
    [Dependency] private readonly StatusEffectsSystem _status = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly SharedStealthSystem _stealth = default!;

    private readonly HashSet<Entity<HumanoidProfileComponent>> _lookupHumanoid = new();

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<LastRefugeComponent, BeforeCastSpellEvent>(OnMagicAttempt);
        SubscribeLocalEvent<LastRefugeComponent, InteractionAttemptEvent>(OnInteractAttempt);
        SubscribeLocalEvent<LastRefugeComponent, GettingInteractedWithAttemptEvent>(OnInteractWithAttempt);
        SubscribeLocalEvent<LastRefugeComponent, AttackAttemptEvent>(OnAttackAttempt);
        SubscribeLocalEvent<LastRefugeComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<LastRefugeComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<LastRefugeComponent, BeforeDamageChangedEvent>(OnBeforeDamageChanged);
        SubscribeLocalEvent<LastRefugeComponent, PreventCollideEvent>(OnPreventCollide);
        SubscribeLocalEvent<LastRefugeComponent, HitScanReflectAttemptEvent>(OnHitscanReflect);

        SubscribeLocalEvent<LastRefugeActionComponent, BeforeCastSpellEvent>(OnActionMagicAttempt);
    }

    private void OnHitscanReflect(Entity<LastRefugeComponent> ent, ref HitScanReflectAttemptEvent args)
    {
        args.Reflected = true;
    }

    private void OnPreventCollide(Entity<LastRefugeComponent> ent, ref PreventCollideEvent args)
    {
        if (HasComp<ProjectileComponent>(args.OtherEntity) || HasComp<ThrownItemComponent>(args.OtherEntity))
            args.Cancelled = true;
    }

    private void OnBeforeDamageChanged(Entity<LastRefugeComponent> ent, ref BeforeDamageChangedEvent args)
    {
        if (!args.Damage.DamageDict.TryGetValue("Holy", out var dmg) || dmg <= FixedPoint2.Zero)
            return;

        if (_status.TryRemoveStatusEffect(ent, ent.Comp.Status))
            return;

        RemCompDeferred(ent, ent.Comp);
    }

    private void OnShutdown(Entity<LastRefugeComponent> ent, ref ComponentShutdown args)
    {
        if (TerminatingOrDeleted(ent))
            return;

        if (!ent.Comp.HadGodmode)
            RemComp<GodmodeComponent>(ent);

        if (!ent.Comp.HadStealth)
            RemComp<StealthComponent>(ent);

        if (!ent.Comp.HadSlowdownImmunity)
            RemComp<SpeedModifierImmunityComponent>(ent);

        if (ent.Comp.HadStrippable)
            EnsureComp<StrippableComponent>(ent);

        _movement.RefreshMovementSpeedModifiers(ent.Owner);

        var actions = _actions.GetActions(ent);
        foreach (var (actionUid, _) in actions)
        {
            if (HasComp<LastRefugeActionComponent>(actionUid))
                _actions.SetIfBiggerCooldown(actionUid, ent.Comp.Cooldown);
        }
    }

    private void OnStartup(Entity<LastRefugeComponent> ent, ref ComponentStartup args)
    {
        ent.Comp.HadStealth = HasComp<StealthComponent>(ent);
        ent.Comp.HadGodmode = EnsureComp<GodmodeComponent>(ent, out _);
        ent.Comp.HadSlowdownImmunity = EnsureComp<SpeedModifierImmunityComponent>(ent, out _);
        ent.Comp.HadStrippable = RemCompDeferred<StrippableComponent>(ent);
        Dirty(ent);

        _movement.RefreshMovementSpeedModifiers(ent.Owner);

        if (ent.Comp.HadStealth)
            return;

        var stealth = EnsureComp<StealthComponent>(ent);
        stealth.ExamineThreshold = 0f;
        stealth.ExaminedDesc = ent.Comp.ExamineMessage;
        Dirty(ent, stealth);

        _stealth.SetVisibility(ent.Owner, ent.Comp.Visibility, stealth);
        _stealth.SetEnabled(ent.Owner, true, stealth);
    }

    private void OnActionMagicAttempt(Entity<LastRefugeActionComponent> ent, ref BeforeCastSpellEvent args)
    {
        var coords = Transform(args.Performer).Coordinates;
        _lookupHumanoid.Clear();
        _lookup.GetEntitiesInRange(coords, ent.Comp.OtherMindsCheckRange, _lookupHumanoid);

        foreach (var (uid, _) in _lookupHumanoid)
        {
            if (uid == args.Performer || !_mobState.IsAlive(uid))
                continue;

            _popup.PopupEntity(Loc.GetString("heretic-ability-fail-other-minds-nearby"), args.Performer, args.Performer);
            args.Cancelled = true;
            break;
        }
    }

    private void OnAttackAttempt(Entity<LastRefugeComponent> ent, ref AttackAttemptEvent args)
    {
        args.Cancel();
    }

    private void OnInteractWithAttempt(Entity<LastRefugeComponent> ent, ref GettingInteractedWithAttemptEvent args)
    {
        args.Cancelled = true;
    }

    private void OnInteractAttempt(Entity<LastRefugeComponent> ent, ref InteractionAttemptEvent args)
    {
        args.Cancelled = true;
    }

    private void OnMagicAttempt(Entity<LastRefugeComponent> ent, ref BeforeCastSpellEvent args)
    {
        args.Cancelled = true;
    }
}
