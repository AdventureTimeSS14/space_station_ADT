using Robust.Shared.Serialization;

namespace Content.Shared.ADT.SeedDna;

[Serializable, NetSerializable]
public sealed class SeedDnaGeneTransferMessage(
    string geneId,
    SeedDnaTransferDirection direction
) : BoundUserInterfaceMessage
{
    public readonly string GeneId = geneId;
    public readonly SeedDnaTransferDirection Direction = direction;
}

[Serializable, NetSerializable]
public enum SeedDnaTransferDirection : byte
{
    SeedToDisk,
    DiskToSeed,
}

[Serializable, NetSerializable]
public sealed class SeedDnaSellMessage : BoundUserInterfaceMessage
{
}
