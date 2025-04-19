using Content.Server.Botany.Components;
using Content.Shared.Atmos;
using Content.Shared.EntityEffects;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using System.Linq;

namespace Content.Server.EntityEffects.Effects;

/// <summary>
///     changes the gases that a plant or produce create.
/// </summary>
public sealed partial class PlantMutateExudeGasses : EntityEffect
{
    [DataField]
    public float MinValue = 0.01f;

    [DataField]
    public float MaxValue = 0.5f;

    // ADT-Tweak-Start
    private static readonly Dictionary<Gas, float> GasWeights = new()
    {
        // Standard chance
        { Gas.CarbonDioxide, 1.0f },
        { Gas.WaterVapor,    1.0f },
        { Gas.Ammonia,       1.0f },
        { Gas.NitrousOxide,  1.0f },
        { Gas.Oxygen,        0.8f },
        { Gas.Nitrogen,      0.8f },

        // Rare chance
        { Gas.Healium,       0.7f },
        { Gas.Nitrium,       0.7f },
        { Gas.BZ,            0.6f },
        { Gas.Pluoxium,      0.6f },
        { Gas.Helium,        0.6f },
        { Gas.Hydrogen,      0.5f },
        { Gas.Plasma,        0.5f },
        { Gas.ProtoNitrate,  0.5f },
        { Gas.Tritium,       0.5f },
        { Gas.HyperNoblium,  0.4f },
        { Gas.Halon,         0.4f },

        // Minimum chance
        { Gas.Zauker,        0.3f },
        { Gas.Frezon,        0.2f },
        { Gas.AntiNoblium,   0.1f },
    };
    // ADT-Tweak-End

    public override void Effect(EntityEffectBaseArgs args)
    {
        var plantholder = args.EntityManager.GetComponent<PlantHolderComponent>(args.TargetEntity);

        if (plantholder.Seed == null)
            return;

        var random = IoCManager.Resolve<IRobustRandom>();
        var gasses = plantholder.Seed.ExudeGasses;

        float amount = random.NextFloat(MinValue, MaxValue);
        Gas gas = PickWeightedGas(random);

        if (gasses.ContainsKey(gas))
        {
            gasses[gas] += amount;
        }
        else
        {
            gasses.Add(gas, amount);
        }
    }

    // ADT-Tweak-Start
    private static Gas PickWeightedGas(IRobustRandom random)
    {
        var totalWeight = GasWeights.Values.Sum();
        var pick = random.NextFloat(0f, totalWeight);
        float cumulative = 0f;

        foreach (var (gas, weight) in GasWeights)
        {
            cumulative += weight;
            if (pick <= cumulative)
                return gas;
        }

        return Gas.Oxygen; // fallback
    }
    // ADT-Tweak-End

    protected override string? ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
    {
        return "TODO";
    }
}

/// <summary>
///     changes the gases that a plant or produce consumes.
/// </summary>
public sealed partial class PlantMutateConsumeGasses : EntityEffect
{
    [DataField]
    public float MinValue = 0.01f;

    [DataField]
    public float MaxValue = 0.5f;

    // ADT-Tweak-Start
    private static readonly Dictionary<Gas, float> GasWeights = new()
    {
        // Standard chance
        { Gas.CarbonDioxide, 1.0f },
        { Gas.WaterVapor,    1.0f },
        { Gas.Ammonia,       1.0f },
        { Gas.NitrousOxide,  1.0f },
        { Gas.Oxygen,        0.8f },
        { Gas.Nitrogen,      0.8f },

        // Rare chance
        { Gas.Healium,       0.7f },
        { Gas.Nitrium,       0.7f },
        { Gas.BZ,            0.6f },
        { Gas.Pluoxium,      0.6f },
        { Gas.Helium,        0.6f },
        { Gas.Hydrogen,      0.5f },
        { Gas.Plasma,        0.5f },
        { Gas.ProtoNitrate,  0.5f },
        { Gas.Tritium,       0.5f },
        { Gas.HyperNoblium,  0.4f },
        { Gas.Halon,         0.4f },

        // Minimum chance
        { Gas.Zauker,        0.3f },
        { Gas.Frezon,        0.2f },
        { Gas.AntiNoblium,   0.1f },
    };
    // ADT-Tweak-End

    public override void Effect(EntityEffectBaseArgs args)
    {
        var plantholder = args.EntityManager.GetComponent<PlantHolderComponent>(args.TargetEntity);

        if (plantholder.Seed == null)
            return;

        var random = IoCManager.Resolve<IRobustRandom>();
        var gasses = plantholder.Seed.ConsumeGasses;

        float amount = random.NextFloat(MinValue, MaxValue);
        Gas gas = PickWeightedGas(random);

        if (gasses.ContainsKey(gas))
        {
            gasses[gas] += amount;
        }
        else
        {
            gasses.Add(gas, amount);
        }
    }

    // ADT-Tweak-Start
    private static Gas PickWeightedGas(IRobustRandom random)
    {
        var totalWeight = GasWeights.Values.Sum();
        var pick = random.NextFloat(0f, totalWeight);
        float cumulative = 0f;

        foreach (var (gas, weight) in GasWeights)
        {
            cumulative += weight;
            if (pick <= cumulative)
                return gas;
        }

        return Gas.Oxygen; // fallback
    }
    // ADT-Tweak-End

    protected override string? ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
    {
        return "TODO";
    }
}