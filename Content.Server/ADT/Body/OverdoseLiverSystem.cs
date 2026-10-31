using Content.Shared.ADT.Body.OrganHealth;
using Content.Shared.Body.Components;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.EntityConditions.Conditions;
using Content.Shared.EntityConditions.Conditions.Body;
using Content.Shared.EntityEffects;
using Content.Shared.EntityEffects.Effects;
using Content.Shared.EntityEffects.Effects.Damage;
using Content.Shared.FixedPoint;
using Content.Shared.Mobs.Systems;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Server.ADT.Body;

public sealed class OverdoseLiverSystem : EntitySystem
{
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private IPrototypeManager _prototype = default!;
    [Dependency] private MobStateSystem _mobState = default!;
    [Dependency] private OrganHealthSystem _organHealth = default!;

    private static readonly ProtoId<Content.Shared.Body.OrganCategoryPrototype> Liver = "Liver";
    private static readonly TimeSpan UpdateInterval = TimeSpan.FromSeconds(2);
    private const float DamagePerReagent = 0.2f;

    private readonly Dictionary<string, FixedPoint2?> _thresholds = new();
    private TimeSpan _nextUpdate;

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (_timing.CurTime < _nextUpdate)
            return;

        _nextUpdate = _timing.CurTime + UpdateInterval;

        var query = EntityQueryEnumerator<OrganFunctionsComponent, BloodstreamComponent>();
        while (query.MoveNext(out var uid, out _, out var blood))
        {
            if (blood.BloodSolution is not { } solution || _mobState.IsDead(uid))
                continue;

            var overdosed = 0;
            foreach (var (reagent, _) in solution.Comp.Solution.GetReagentPrototypes(_prototype))
            {
                if (GetThreshold(reagent) is not { } threshold)
                    continue;

                if (solution.Comp.Solution.GetTotalPrototypeQuantity(reagent.ID) >= threshold)
                    overdosed++;
            }

            if (overdosed > 0)
                _organHealth.ChangeDamage(uid, Liver, DamagePerReagent * overdosed, OrganKind.Organic);
        }
    }

    private FixedPoint2? GetThreshold(ReagentPrototype reagent)
    {
        if (_thresholds.TryGetValue(reagent.ID, out var cached))
            return cached;

        FixedPoint2? result = null;
        if (reagent.Metabolisms != null)
        {
            foreach (var entry in reagent.Metabolisms.Metabolisms.Values)
            {
                foreach (var effect in entry.Effects)
                {
                    if (effect is AdjustOrganDamage { Category.Id: "Liver", Amount: > 0 })
                    {
                        _thresholds[reagent.ID] = null;
                        return null;
                    }
                }
            }

            foreach (var entry in reagent.Metabolisms.Metabolisms.Values)
            {
                foreach (var effect in entry.Effects)
                {
                    if (!IsHarmful(effect) || effect.Conditions == null)
                        continue;

                    var typeSpecific = false;
                    FixedPoint2? min = null;
                    foreach (var condition in effect.Conditions)
                    {
                        if (condition is MetabolizerTypeCondition { Inverted: false })
                            typeSpecific = true;
                        else if (condition is ReagentCondition own && own.Reagent == reagent.ID && own.Min > FixedPoint2.Zero)
                            min = min == null ? own.Min : FixedPoint2.Max(min.Value, own.Min);
                    }

                    if (typeSpecific || min == null)
                        continue;

                    result = result == null ? min : FixedPoint2.Min(result.Value, min.Value);
                }
            }
        }

        _thresholds[reagent.ID] = result;
        return result;
    }

    private static bool IsHarmful(EntityEffect effect)
    {
        return effect switch
        {
            HealthChange change => change.Damage.GetTotal() > FixedPoint2.Zero,
            _ => false,
        };
    }
}
