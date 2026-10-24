using Content.Shared.Body;
using Content.Shared.Body.Systems;
using Content.Shared.Chemistry.Components;
using Content.Shared.Damage;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Systems;
using Content.Shared.FixedPoint;
using Content.Shared.Fluids;
using Content.Shared.Humanoid;
using Content.Shared.Mobs.Systems;
using Content.Shared.Movement.Systems;
using Content.Shared.Popups;
using Content.Shared.Weapons.Melee.Events;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server.ADT.Lavaland.MarrowWeaver;

public sealed class ADTMarrowWeaverSystem : EntitySystem
{
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private BloodstreamSystem _bloodstream = default!;
    [Dependency] private IPrototypeManager _proto = default!;
    [Dependency] private BodySystem _body = default!;
    [Dependency] private DamageableSystem _damageable = default!;
    [Dependency] private MobStateSystem _mobState = default!;
    [Dependency] private MobThresholdSystem _thresholds = default!;
    [Dependency] private MovementSpeedModifierSystem _movement = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private SharedPuddleSystem _puddle = default!;
    [Dependency] private IRobustRandom _random = default!;

    private readonly List<EntityUid> _eaten = new();

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
        _movement.RefreshMovementSpeedModifiers(ent);

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

            if (_mobState.IsDead(target) && HasComp<HumanoidAppearanceComponent>(target) && IsHurt(ent))
                Feed(ent, target);
        }
    }

    private void Feed(Entity<ADTMarrowWeaverComponent> ent, EntityUid victim)
    {
        if (HasComp<ADTMarrowWeaverEatenComponent>(victim))
            return;

        _eaten.Clear();
        foreach (var organ in _body.EnumerateOrgans<OrganComponent>(victim))
        {
            if (organ.Comp1.Category is { } category && ent.Comp.EatenOrgans.Contains(category))
                _eaten.Add(organ.Owner);
        }

        EnsureComp<ADTMarrowWeaverEatenComponent>(victim);

        if (_eaten.Count == 0)
        {
            _popup.PopupEntity(Loc.GetString(ent.Comp.NoOrgansPopup), ent, ent);
            return;
        }

        foreach (var organ in _eaten)
        {
            QueueDel(organ);
        }

        var coords = Transform(victim).Coordinates;
        _puddle.TrySpillAt(coords, new Solution(ent.Comp.FeedBloodReagent, ent.Comp.FeedBlood), out _, false);
        _audio.PlayPvs(ent.Comp.FeedSound, coords);

        _popup.PopupEntity(Loc.GetString(ent.Comp.FeedPopup, ("weaver", ent.Owner), ("victim", victim)), ent, Filter.PvsExcept(ent), true, PopupType.MediumCaution);
        _popup.PopupEntity(Loc.GetString(ent.Comp.FeedPopupSelf, ("victim", victim)), ent, ent, PopupType.Medium);

        _damageable.HealEvenly(ent.Owner, -ent.Comp.FeedHeal);
    }

    private bool IsHurt(EntityUid uid)
    {
        return TryComp<DamageableComponent>(uid, out var damageable) && damageable.TotalDamage > 0;
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
