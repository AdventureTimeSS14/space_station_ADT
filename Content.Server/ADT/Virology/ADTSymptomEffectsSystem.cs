using System.Linq;
using Content.Shared.ADT.Virology;
using Content.Shared.ADT.Virology.Components;
using Content.Shared.ADT.Virology.Effects;
using Content.Shared.Body.Components;
using Content.Shared.Body.Systems;
using Content.Shared.Chemistry.Components;
using Content.Shared.Damage;
using Content.Shared.Damage.Systems;
using Content.Shared.Popups;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server.ADT.Virology;

public sealed class ADTSymptomEffectsSystem : EntitySystem
{
    [Dependency] private IPrototypeManager _proto = default!;
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private ADTDiseaseSystem _disease = default!;
    [Dependency] private BloodstreamSystem _bloodstream = default!;
    [Dependency] private DamageableSystem _damageable = default!;
    [Dependency] private SharedPopupSystem _popup = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ADTDiseaseCarrierComponent, ADTSymptomEvent<ADTSymptomHeal>>(OnHeal);
        SubscribeLocalEvent<ADTDiseaseCarrierComponent, ADTSymptomEvent<ADTSymptomCureDiseases>>(OnCureDiseases);
        SubscribeLocalEvent<ADTDiseaseCarrierComponent, ADTSymptomEvent<ADTSymptomLongevity>>(OnLongevity);
        SubscribeLocalEvent<ADTDiseaseCarrierComponent, ADTSymptomEvent<ADTSymptomAddReagent>>(OnAddReagent);
    }

    private void OnHeal(Entity<ADTDiseaseCarrierComponent> ent, ref ADTSymptomEvent<ADTSymptomHeal> args)
    {
        var effect = args.Effect;
        var stageSpeed = GetTotalStageSpeed(args.Disease);
        var magnitude = effect.Base + stageSpeed;
        if (magnitude < 0f)
            magnitude = 0f;

        var amount = MathF.Sqrt(magnitude) * (1f + _random.NextFloat()) * effect.Scale;
        _damageable.TryChangeDamage(ent.Owner, effect.Damage * amount, ignoreResistances: true, interruptsDoAfters: false);
    }

    private void OnCureDiseases(Entity<ADTDiseaseCarrierComponent> ent, ref ADTSymptomEvent<ADTSymptomCureDiseases> args)
    {
        var carrier = ent.Comp;
        var state = args.State;

        if (args.Phase == ADTSymptomPhase.End)
        {
            if (state.Text != null)
            {
                foreach (var id in state.Text.Split('\n'))
                {
                    carrier.Resistances.Remove(id);
                }

                state.Text = null;
            }

            if (args.Effect.EndPopup is { } endPopup)
                _popup.PopupEntity(Loc.GetString(endPopup), ent, ent);

            return;
        }

        var cured = new List<string>();
        foreach (var disease in carrier.Diseases.ToList())
        {
            if (ReferenceEquals(disease, args.Disease))
                continue;

            if (_proto.TryIndex(disease.Prototype, out var proto) && proto.VirusHealResistant)
                continue;

            cured.Add(disease.GetId());
            _disease.Cure((ent.Owner, carrier), disease);
        }

        if (cured.Count == 0)
            return;

        var known = state.Text?.Split('\n').ToList() ?? new List<string>();
        known.AddRange(cured);
        state.Text = string.Join('\n', known.Distinct());

        if (args.Effect.CuredPopup is { } popup)
            _popup.PopupEntity(Loc.GetString(popup), ent, ent);
    }

    private void OnLongevity(Entity<ADTDiseaseCarrierComponent> ent, ref ADTSymptomEvent<ADTSymptomLongevity> args)
    {
        var state = args.State;

        if (args.Phase == ADTSymptomPhase.Start)
        {
            var effect = args.Effect;
            state.Counter = _random.Next(effect.Longevity - effect.Variation, effect.Longevity + effect.Variation + 1);
            return;
        }

        if (args.Phase != ADTSymptomPhase.Activate)
            return;

        if (state.Counter <= 0)
            state.Counter = args.Effect.Longevity;

        state.Counter--;
        if (state.Counter > 0)
            return;

        _disease.Cure((ent.Owner, ent.Comp), args.Disease);
    }

    private void OnAddReagent(Entity<ADTDiseaseCarrierComponent> ent, ref ADTSymptomEvent<ADTSymptomAddReagent> args)
    {
        if (!HasComp<BloodstreamComponent>(ent))
            return;

        var effect = args.Effect;
        if (effect.MaxExisting < float.MaxValue
            && _disease.GetReagentAmount(ent, effect.Reagent).Float() >= effect.MaxExisting)
        {
            return;
        }

        var solution = new Solution(effect.Reagent, effect.Amount);
        _bloodstream.TryAddToBloodstream((ent.Owner, null), solution);
    }

    private int GetTotalStageSpeed(ADTDisease disease)
    {
        var total = 0;
        foreach (var id in disease.Symptoms)
        {
            if (_proto.TryIndex(id, out var symptom))
                total += symptom.StageSpeed;
        }

        return total;
    }
}
