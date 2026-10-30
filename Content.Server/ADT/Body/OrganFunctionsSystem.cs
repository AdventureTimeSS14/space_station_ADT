using Content.Server.ADT.Deafness;
using Content.Server.Body.Systems;
using Content.Shared.ADT.Body.OrganHealth;
using Content.Shared.ADT.Deafness;
using Content.Shared.ADT.Salvage.Components;
using Content.Shared.Body;
using Content.Shared.Body.Components;
using Content.Shared.Damage;
using Content.Shared.Damage.Systems;
using Content.Shared.Eye.Blinding.Systems;
using Content.Shared.Jittering;
using Content.Shared.Medical;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Popups;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server.ADT.Body;

public sealed class OrganFunctionsSystem : EntitySystem
{
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private BodySystem _body = default!;
    [Dependency] private OrganHealthSystem _organHealth = default!;
    [Dependency] private DamageableSystem _damageable = default!;
    [Dependency] private MobStateSystem _mobState = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private SharedJitteringSystem _jittering = default!;
    [Dependency] private VomitSystem _vomit = default!;
    [Dependency] private BlindableSystem _blindable = default!;
    [Dependency] private BlurryVisionSystem _blurry = default!;
    [Dependency] private ADTDeafnessSystem _deafness = default!;

    private static readonly ProtoId<OrganCategoryPrototype> Heart = "Heart";
    private static readonly ProtoId<OrganCategoryPrototype> Lungs = "Lungs";
    private static readonly ProtoId<OrganCategoryPrototype> Liver = "Liver";
    private static readonly ProtoId<OrganCategoryPrototype> Kidneys = "Kidneys";
    private static readonly ProtoId<OrganCategoryPrototype> Stomach = "Stomach";
    private static readonly ProtoId<OrganCategoryPrototype> Eyes = "Eyes";
    private static readonly ProtoId<OrganCategoryPrototype> Ears = "Ears";
    private static readonly ProtoId<OrganCategoryPrototype> Tongue = "Tongue";

    private static readonly TimeSpan HeartAttackSecondStage = TimeSpan.FromSeconds(15);
    private static readonly TimeSpan HeartAttackThirdStage = TimeSpan.FromSeconds(30);
    private static readonly TimeSpan DefibrillatorStrainWindow = TimeSpan.FromSeconds(60);
    private const int DefibrillatorStrainFreeZaps = 3;
    private const float DefibrillatorStrainDamage = 15f;
    private const float DamagedEyesBlur = 2f;
    private const float FailingStomachVomitChance = 0.05f;
    private const float DamagedHeartArrhythmiaChance = 0.2f;
    private const float DamagedStomachVomitChance = 0.02f;
    private const float DamagedEarsRingingChance = 0.04f;
    private const float EarsRingingSeverity = 2.5f;
    private const float LiverVomitChance = 0.15f;
    private const float LiverCollateralDamage = 0.4f;
    private static readonly TimeSpan LiverVomitingStage = TimeSpan.FromSeconds(60);
    private static readonly TimeSpan LiverCollateralStage = TimeSpan.FromSeconds(120);
    private static readonly TimeSpan LiverCriticalStage = TimeSpan.FromSeconds(180);

    private static readonly ProtoId<OrganCategoryPrototype>[] LiverCollateralOrgans = { "Heart", "Lungs", "Stomach", "Eyes", "Ears" };

    private static readonly DamageSpecifier ArrhythmiaDamage = Asphyxiation(1);
    private static readonly DamageSpecifier HeartAttackDamage = Asphyxiation(3);
    private static readonly DamageSpecifier HeartStoppedDamage = Asphyxiation(8);
    private static readonly DamageSpecifier DamagedLiverDamage = Poison(0.6f);
    private static readonly DamageSpecifier FailingLiverDamage = Poison(2);
    private static readonly DamageSpecifier CriticalLiverDamage = Poison(3);
    private static readonly DamageSpecifier DamagedKidneysDamage = Poison(0.4f);
    private static readonly DamageSpecifier FailingKidneysDamage = Poison(1);

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<OrganHealthComponent, OrganGotInsertedEvent>(OnOrganInserted);
        SubscribeLocalEvent<OrganHealthComponent, OrganGotRemovedEvent>(OnOrganRemoved);
        SubscribeLocalEvent<OrganHealthComponent, OrganStageChangedEvent>(OnStageChanged);
        SubscribeLocalEvent<OrganHealthComponent, BodyRelayedEvent<CanMetabolizeGasEvent>>(OnCanMetabolizeGas,
            after: new[] { typeof(RespiratorSystem) });
        SubscribeLocalEvent<OrganFunctionsComponent, TargetDefibrillatedEvent>(OnDefibrillated);
    }

    private void OnOrganInserted(Entity<OrganHealthComponent> ent, ref OrganGotInsertedEvent args)
    {
        if (!HasComp<MobStateComponent>(args.Target))
            return;

        var functions = EnsureComp<OrganFunctionsComponent>(args.Target);
        if (CompOrNull<OrganComponent>(ent)?.Category is { } category)
            functions.ExpectedCategories.Add(category);

        functions.NextUpdate = _timing.CurTime;
    }

    private void OnOrganRemoved(Entity<OrganHealthComponent> ent, ref OrganGotRemovedEvent args)
    {
        if (TryComp<OrganFunctionsComponent>(args.Target, out var functions))
            functions.NextUpdate = _timing.CurTime;
    }

    private void OnStageChanged(Entity<OrganHealthComponent> ent, ref OrganStageChangedEvent args)
    {
        if (args.Body is not { } body || !TryComp<OrganFunctionsComponent>(body, out var functions))
            return;

        functions.NextUpdate = _timing.CurTime;

        if (_mobState.IsIncapacitated(body))
            return;

        if (args.NewStage > args.OldStage)
            _popup.PopupEntity(Loc.GetString("organ-health-stage-worse", ("organ", ent.Owner)), body, body, PopupType.SmallCaution);
        else if (args.NewStage == OrganStage.Healthy)
            _popup.PopupEntity(Loc.GetString("organ-health-stage-healed", ("organ", ent.Owner)), body, body, PopupType.Small);
    }

    private void OnCanMetabolizeGas(Entity<OrganHealthComponent> ent, ref BodyRelayedEvent<CanMetabolizeGasEvent> args)
    {
        if (!HasComp<LungComponent>(ent))
            return;

        var saturation = ent.Comp.Stage switch
        {
            OrganStage.Failing => 0f,
            OrganStage.Damaged => args.Args.Saturation * 0.5f,
            _ => args.Args.Saturation,
        };

        args.Args = args.Args with { Saturation = saturation };
    }

    private void OnDefibrillated(Entity<OrganFunctionsComponent> ent, ref TargetDefibrillatedEvent args)
    {
        var now = _timing.CurTime;
        ent.Comp.DefibrillatorZaps.RemoveAll(zap => now - zap > DefibrillatorStrainWindow);
        ent.Comp.DefibrillatorZaps.Add(now);

        var hasHeart = _organHealth.TryGetOrgan(ent, Heart, out var heart);
        if (hasHeart && ent.Comp.DefibrillatorZaps.Count > DefibrillatorStrainFreeZaps)
            _organHealth.ChangeDamage((heart.Owner, heart.Comp), DefibrillatorStrainDamage);

        if (ent.Comp.HeartAttackStart == null || !hasHeart || heart.Comp.Stage == OrganStage.Failing)
            return;

        ent.Comp.HeartAttackStart = null;
        _popup.PopupEntity(Loc.GetString("organ-heart-attack-stopped"), ent, ent, PopupType.Medium);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var now = _timing.CurTime;
        var query = EntityQueryEnumerator<OrganFunctionsComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if (now < comp.NextUpdate)
                continue;

            comp.NextUpdate = now + comp.UpdateInterval;

            var alive = !_mobState.IsDead(uid);
            if (alive)
                HealOrgans(uid, (float) comp.UpdateInterval.TotalSeconds);

            UpdateSenses((uid, comp));

            if (!alive)
            {
                comp.LiverFailingSince = null;
                continue;
            }

            UpdateHeart((uid, comp), now);
            UpdateFiltration(uid, comp);
            UpdateStomach(uid, comp);
            UpdateEars(uid, comp);
        }
    }

    private void HealOrgans(EntityUid body, float seconds)
    {
        foreach (var organ in _body.EnumerateOrgans<OrganHealthComponent>(body))
        {
            var health = organ.Comp2;
            if (health.Kind != OrganKind.Organic || health.Stage == OrganStage.Failing || health.Damage <= 0f)
                continue;

            _organHealth.ChangeDamage((organ.Owner, health), -health.HealRate * seconds);
        }
    }

    private OrganStage? GetCategoryStage(EntityUid body, OrganFunctionsComponent comp, ProtoId<OrganCategoryPrototype> category)
    {
        if (!comp.ExpectedCategories.Contains(category))
            return null;

        return _organHealth.TryGetOrgan(body, category, out var organ) ? organ.Comp.Stage : OrganStage.Failing;
    }

    private void UpdateSenses(Entity<OrganFunctionsComponent> ent)
    {
        var eyes = GetCategoryStage(ent, ent.Comp, Eyes);
        var blind = eyes == OrganStage.Failing;
        var blur = eyes == OrganStage.Damaged ? DamagedEyesBlur : 0f;
        var mute = GetCategoryStage(ent, ent.Comp, Tongue) == OrganStage.Failing;

        if (blind != ent.Comp.Blind || !MathHelper.CloseTo(blur, ent.Comp.Blur) || mute != ent.Comp.Mute)
        {
            var visionChanged = blind != ent.Comp.Blind || !MathHelper.CloseTo(blur, ent.Comp.Blur);
            ent.Comp.Blind = blind;
            ent.Comp.Blur = blur;
            ent.Comp.Mute = mute;
            Dirty(ent);

            if (visionChanged)
            {
                _blindable.UpdateIsBlind(ent.Owner);
                _blurry.UpdateBlurMagnitude(ent.Owner);
            }
        }

        if (GetCategoryStage(ent, ent.Comp, Ears) != OrganStage.Failing)
            return;

        var severity = CompOrNull<ADTDeafenedComponent>(ent)?.Severity ?? 0f;
        var threshold = CompOrNull<ADTDeafenedComponent>(ent)?.TotalThreshold ?? 2f;
        var missing = threshold + 1f - severity;
        if (missing > 0f)
            _deafness.Deafen(ent, missing);
    }

    private void UpdateHeart(Entity<OrganFunctionsComponent> ent, TimeSpan now)
    {
        if (HasComp<CursedHeartComponent>(ent))
        {
            ent.Comp.HeartAttackStart = null;
            return;
        }

        var stage = GetCategoryStage(ent, ent.Comp, Heart);
        if (stage == null)
            return;

        if (stage == OrganStage.Failing && ent.Comp.HeartAttackStart == null)
        {
            ent.Comp.HeartAttackStart = now;
            _popup.PopupEntity(Loc.GetString("organ-heart-attack-start"), ent, ent, PopupType.LargeCaution);
        }

        if (ent.Comp.HeartAttackStart is not { } start)
        {
            if (stage == OrganStage.Damaged && _random.Prob(DamagedHeartArrhythmiaChance))
            {
                _jittering.DoJitter(ent, TimeSpan.FromSeconds(2), true);
                _damageable.TryChangeDamage(ent.Owner, ArrhythmiaDamage, ignoreResistances: true, interruptsDoAfters: false);
            }

            return;
        }

        if (stage == OrganStage.Healthy)
        {
            ent.Comp.HeartAttackStart = null;
            return;
        }

        var elapsed = now - start;
        if (elapsed < HeartAttackSecondStage)
        {
            _jittering.DoJitter(ent, ent.Comp.UpdateInterval, true);
            return;
        }

        var damage = elapsed < HeartAttackThirdStage || now < ent.Comp.HeartAttackSuppressedUntil
            ? HeartAttackDamage
            : HeartStoppedDamage;

        _damageable.TryChangeDamage(ent.Owner, damage, ignoreResistances: true, interruptsDoAfters: false);
    }

    private void UpdateFiltration(EntityUid body, OrganFunctionsComponent comp)
    {
        var liverStage = GetCategoryStage(body, comp, Liver);
        var liver = liverStage switch
        {
            OrganStage.Damaged => DamagedLiverDamage,
            OrganStage.Failing => FailingLiverDamage,
            _ => null,
        };

        if (liverStage == OrganStage.Failing)
        {
            comp.LiverFailingSince ??= _timing.CurTime;
            var failing = _timing.CurTime - comp.LiverFailingSince.Value;

            if (failing >= LiverCriticalStage)
                liver = CriticalLiverDamage;

            if (failing >= LiverVomitingStage && _random.Prob(LiverVomitChance))
                _vomit.Vomit(body);

            if (failing >= LiverCollateralStage)
                _organHealth.ChangeDamage(body, _random.Pick(LiverCollateralOrgans), LiverCollateralDamage, OrganKind.Organic);
        }
        else
        {
            comp.LiverFailingSince = null;
        }

        if (liver != null)
            _damageable.TryChangeDamage(body, liver, ignoreResistances: true, interruptsDoAfters: false);

        var kidneys = GetCategoryStage(body, comp, Kidneys) switch
        {
            OrganStage.Damaged => DamagedKidneysDamage,
            OrganStage.Failing => FailingKidneysDamage,
            _ => null,
        };

        if (kidneys != null)
            _damageable.TryChangeDamage(body, kidneys, ignoreResistances: true, interruptsDoAfters: false);
    }

    private void UpdateStomach(EntityUid body, OrganFunctionsComponent comp)
    {
        if (!comp.ExpectedCategories.Contains(Stomach) || !_organHealth.TryGetOrgan(body, Stomach, out var stomach))
            return;

        var chance = stomach.Comp.Stage switch
        {
            OrganStage.Failing => FailingStomachVomitChance,
            OrganStage.Damaged => DamagedStomachVomitChance,
            _ => 0f,
        };

        if (chance > 0f && _random.Prob(chance))
            _vomit.Vomit(body);
    }

    private void UpdateEars(EntityUid body, OrganFunctionsComponent comp)
    {
        if (GetCategoryStage(body, comp, Ears) != OrganStage.Damaged || !_random.Prob(DamagedEarsRingingChance))
            return;

        _deafness.Deafen(body, EarsRingingSeverity);
        _popup.PopupEntity(Loc.GetString("organ-ears-ringing"), body, body, PopupType.SmallCaution);
    }

    private static DamageSpecifier Asphyxiation(float amount)
    {
        return new DamageSpecifier { DamageDict = new() { { "Asphyxiation", amount } } };
    }

    private static DamageSpecifier Poison(float amount)
    {
        return new DamageSpecifier { DamageDict = new() { { "Poison", amount } } };
    }
}
