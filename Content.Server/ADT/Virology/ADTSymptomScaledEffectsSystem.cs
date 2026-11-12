using Content.Server.Atmos.EntitySystems;
using Content.Shared.ADT.Virology;
using Content.Shared.ADT.Virology.Components;
using Content.Shared.ADT.Virology.Effects;
using Content.Shared.ADT.Virology.Prototypes;
using Content.Shared.Damage;
using Content.Shared.FixedPoint;
using Content.Shared.Damage.Systems;
using Content.Shared.Damage.Components;
using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Item;
using Content.Shared.Temperature.Components;
using Content.Shared.Temperature.Systems;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server.ADT.Virology;

public sealed class ADTSymptomScaledEffectsSystem : EntitySystem
{
    [Dependency] private IPrototypeManager _proto = default!;
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private ADTDiseaseSystem _disease = default!;
    [Dependency] private DamageableSystem _damageable = default!;
    [Dependency] private FlammableSystem _flammable = default!;
    [Dependency] private SharedHandsSystem _hands = default!;
    [Dependency] private SharedTemperatureSystem _temperature = default!;
    [Dependency] private SharedStaminaSystem _stamina = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ADTDiseaseCarrierComponent, ADTSymptomEvent<ADTSymptomScaledDamage>>(OnScaledDamage);
        SubscribeLocalEvent<ADTDiseaseCarrierComponent, ADTSymptomEvent<ADTSymptomScaledTemperature>>(OnScaledTemperature);
        SubscribeLocalEvent<ADTDiseaseCarrierComponent, ADTSymptomEvent<ADTSymptomScaledFire>>(OnScaledFire);
        SubscribeLocalEvent<ADTDiseaseCarrierComponent, ADTSymptomEvent<ADTSymptomSpread>>(OnSpread);
        SubscribeLocalEvent<ADTDiseaseCarrierComponent, ADTSymptomEvent<ADTSymptomDropItem>>(OnDropItem);
        SubscribeLocalEvent<ADTDiseaseCarrierComponent, ADTSymptomEvent<ADTSymptomStamina>>(OnStamina);
    }

    private void OnScaledDamage(Entity<ADTDiseaseCarrierComponent> ent, ref ADTSymptomEvent<ADTSymptomScaledDamage> args)
    {
        var amount = Calculate(args.Effect, args.Disease);
        if (amount == 0f)
            return;

        _damageable.TryChangeDamage(ent.Owner, args.Effect.Damage * amount, ignoreResistances: true, interruptsDoAfters: false);
    }

    private void OnScaledTemperature(Entity<ADTDiseaseCarrierComponent> ent, ref ADTSymptomEvent<ADTSymptomScaledTemperature> args)
    {
        if (!TryComp<TemperatureComponent>(ent, out var temperature))
            return;

        var effect = args.Effect;
        if (effect.MaxTemperature is { } max && temperature.Temperature >= max)
            return;

        if (effect.MinTemperature is { } min && temperature.Temperature <= min)
            return;

        var amount = Calculate(effect, args.Disease);
        if (amount == 0f)
            return;

        _temperature.ChangeHeat((ent.Owner, temperature), effect.Heat ? amount : -amount, true);
    }

    private void OnScaledFire(Entity<ADTDiseaseCarrierComponent> ent, ref ADTSymptomEvent<ADTSymptomScaledFire> args)
    {
        var stacks = Calculate(args.Effect, args.Disease);
        if (stacks <= 0f)
            return;

        _flammable.AdjustFireStacks(ent, stacks, ignite: args.Effect.Ignite);

        if (args.Effect.DamageRatio <= 0f)
            return;

        var damage = new DamageSpecifier();
        damage.DamageDict.Add("Heat", FixedPoint2.New(stacks * args.Effect.DamageRatio));
        _damageable.TryChangeDamage(ent.Owner, damage, ignoreResistances: true, interruptsDoAfters: false);
    }

    private void OnSpread(Entity<ADTDiseaseCarrierComponent> ent, ref ADTSymptomEvent<ADTSymptomSpread> args)
    {
        _disease.Spread(ent, args.Disease, args.Effect.Range);
    }

    private void OnDropItem(Entity<ADTDiseaseCarrierComponent> ent, ref ADTSymptomEvent<ADTSymptomDropItem> args)
    {
        if (!TryComp<HandsComponent>(ent, out var hands))
            return;

        var active = _hands.GetActiveItem((ent.Owner, hands));
        if (active == null)
            return;

        if (!TryComp<ItemComponent>(active, out var item) || item.Size != "Tiny")
            return;

        _hands.TryDrop((ent.Owner, hands), active.Value, checkActionBlocker: false);
    }

    private void OnStamina(Entity<ADTDiseaseCarrierComponent> ent, ref ADTSymptomEvent<ADTSymptomStamina> args)
    {
        if (!HasComp<StaminaComponent>(ent))
            return;

        _stamina.TakeStaminaDamage(ent, args.Effect.Amount, visual: false);
    }

    private float Calculate<T>(ADTSymptomScaledEffect<T> effect, ADTDisease disease) where T : ADTSymptomScaledEffect<T>
    {
        var total = 0f;
        foreach (var term in effect.Terms)
        {
            var value = term.Base;
            if (term.Stat is { } stat)
                value += term.Factor * GetStat(disease, stat);

            if (term.Sqrt)
                value = value > 0f ? MathF.Sqrt(value) : 0f;

            total += value * term.Multiplier;
        }

        total *= effect.Multiplier;

        if (effect.Randomize)
            total *= 1f + _random.NextFloat();

        if (effect.ScaleByStage)
            total *= disease.Stage;

        if (effect.Minimum > float.MinValue)
            total = MathF.Max(total, effect.Minimum);

        return total;
    }

    private int GetStat(ADTDisease disease, ADTSymptomStat stat)
    {
        var total = 0;
        foreach (var id in disease.Symptoms)
        {
            if (!_proto.TryIndex<ADTSymptomPrototype>(id, out var symptom))
                continue;

            total += stat switch
            {
                ADTSymptomStat.StageSpeed => symptom.StageSpeed,
                ADTSymptomStat.Stealth => symptom.Stealth,
                ADTSymptomStat.Resistance => symptom.Resistance,
                ADTSymptomStat.Transmittable => symptom.Transmittable,
                _ => 0,
            };
        }

        return total;
    }
}
