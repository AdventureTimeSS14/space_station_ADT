using Content.Shared.Body.Systems;
using Content.Shared.Chemistry.Components;
using Content.Shared.Damage;
using Content.Shared.Damage.Systems;
using Content.Shared.Damage.Components;
using Content.Shared.FixedPoint;
using Content.Shared.Mobs.Systems;
using Content.Shared.Movement.Systems;
using Content.Shared.Popups;
using Content.Shared.Weapons.Melee.Events;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server.ADT.Lavaland.MarrowWeaver;

public sealed class ADTMarrowWeaverSystem : EntitySystem
{
    [Dependency] private BloodstreamSystem _bloodstream = default!;
    [Dependency] private IPrototypeManager _proto = default!;
    [Dependency] private MobStateSystem _mobState = default!;
    [Dependency] private MobThresholdSystem _thresholds = default!;
    [Dependency] private MovementSpeedModifierSystem _movement = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private IRobustRandom _random = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ADTMarrowWeaverComponent, DamageChangedEvent>(OnDamageChanged);
        SubscribeLocalEvent<ADTMarrowWeaverComponent, MeleeHitEvent>(OnMeleeHit);
        SubscribeLocalEvent<ADTMarrowWeaverComponent, RefreshMovementSpeedModifiersEvent>(OnRefreshSpeed);
    }

    private void OnDamageChanged(Entity<ADTMarrowWeaverComponent> ent, ref DamageChangedEvent args)
    {
        if (_mobState.IsDead(ent) || GetHealthFraction(ent) is not { } health)
            return;

        if (!ent.Comp.Angry && args.DamageIncreased && health < ent.Comp.AngryThreshold)
            SetAngry(ent, true);
        else if (ent.Comp.Angry && health > ent.Comp.CalmThreshold)
            SetAngry(ent, false);
    }

    private void SetAngry(Entity<ADTMarrowWeaverComponent> ent, bool angry)
    {
        ent.Comp.Angry = angry;
        _movement.RefreshMovementSpeedModifiers(ent.Owner);

        var popup = angry ? ent.Comp.AngryPopup : ent.Comp.CalmPopup;
        _popup.PopupEntity(Loc.GetString(popup, ("weaver", ent.Owner)), ent, angry ? PopupType.MediumCaution : PopupType.Small);
    }

    private void OnRefreshSpeed(Entity<ADTMarrowWeaverComponent> ent, ref RefreshMovementSpeedModifiersEvent args)
    {
        if (ent.Comp.Angry)
            args.ModifySpeed(ent.Comp.AngrySpeedMultiplier, ent.Comp.AngrySpeedMultiplier);
    }

    private void OnMeleeHit(Entity<ADTMarrowWeaverComponent> ent, ref MeleeHitEvent args)
    {
        if (!args.IsHit || args.HitEntities.Count == 0)
            return;

        var min = ent.Comp.Angry ? ent.Comp.AngryMinDamage : ent.Comp.CalmMinDamage;
        var max = ent.Comp.Angry ? ent.Comp.AngryMaxDamage : ent.Comp.CalmMaxDamage;
        var bonus = _random.Next((int) min, (int) max + 1) - args.BaseDamage.GetTotal().Float();
        if (bonus != 0f)
            args.BonusDamage += new DamageSpecifier(_proto.Index(ent.Comp.DamageType), FixedPoint2.New(bonus));

        var poison = ent.Comp.Angry ? ent.Comp.AngryPoison : ent.Comp.CalmPoison;
        var amount = ent.Comp.Angry ? ent.Comp.AngryPoisonAmount : ent.Comp.CalmPoisonAmount;

        foreach (var target in args.HitEntities)
        {
            _bloodstream.TryAddToBloodstream(target, new Solution(poison, amount));
        }
    }

    private float? GetHealthFraction(EntityUid uid)
    {
        if (!TryComp<DamageableComponent>(uid, out var damageable) ||
            !_thresholds.TryGetDeadThreshold(uid, out var dead) ||
            dead is not { } max || max <= 0)
        {
            return null;
        }

        return 1f - (damageable.TotalDamage / max).Float();
    }
}
