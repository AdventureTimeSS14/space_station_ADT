using Robust.Shared.Serialization;

// ReSharper disable once CheckNamespace
namespace Content.Shared.ADT.SeedDna;

[Serializable, NetSerializable]
public sealed class WriteToTargetSeedDataMessage(
    TargetSeedData target,
    SeedDataDto seedDataDto
) : BoundUserInterfaceMessage
{
    public readonly TargetSeedData Target = target;
    public readonly SeedDataDto SeedDataDto = seedDataDto;
}

[Serializable, NetSerializable]
public enum TargetSeedData : byte
{
    Seed,
    DnaDisk,
}
