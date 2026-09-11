using Robust.Shared.Serialization;

namespace Content.Shared.ADT.SeedDna;

[Serializable, NetSerializable]
public sealed class SeedDnaGeneTransferMessage(
    string geneId,
    SeedDnaTransferDirection direction,
    bool all
) : BoundUserInterfaceMessage
{
    public readonly string GeneId = geneId;
    public readonly SeedDnaTransferDirection Direction = direction;
    public readonly bool All = all;
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
