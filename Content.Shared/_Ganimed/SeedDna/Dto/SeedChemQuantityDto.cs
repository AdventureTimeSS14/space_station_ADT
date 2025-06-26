using Robust.Shared.Serialization;

// ReSharper disable once CheckNamespace
namespace Content.Shared._Ganimed.SeedDna;

// copy-paste from <Content.Server.Botany.SeedChemQuantity>
[DataDefinition]
[Serializable, NetSerializable]
public partial struct SeedChemQuantityDto
{
    /// <summary>
    /// Minimum amount of chemical that is added to produce, regardless of the potency
    /// </summary>
    [DataField("Min")] public int Min;

    /// <summary>
    /// Maximum amount of chemical that can be produced after taking plant potency into account.
    /// </summary>
    [DataField("Max")] public int Max;

    /// <summary>
    /// When chemicals are added to produce, the potency of the seed is divided with this value. Final chemical amount is the result plus the `Min` value.
    /// Example: PotencyDivisor of 20 with seed potency of 55 results in 2.75, 55/20 = 2.75. If minimum is 1 then final result will be 3.75 of that chemical, 55/20+1 = 3.75.
    /// </summary>
    [DataField("PotencyDivisor")] public int PotencyDivisor;

    /// <summary>
    /// Inherent chemical is one that is NOT result of mutation or crossbreeding. These chemicals are removed if species mutation is executed.
    /// </summary>
    [DataField("Inherent")] public bool Inherent = true;

    public bool Equals(SeedChemQuantityDto other)
    {
        return Min == other.Min
               && Max == other.Max
               && PotencyDivisor == other.PotencyDivisor
               && Inherent == other.Inherent;
    }

    public override bool Equals(object? obj)
    {
        return obj is SeedChemQuantityDto other && Equals(other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Min, Max, PotencyDivisor, Inherent);
    }
}
