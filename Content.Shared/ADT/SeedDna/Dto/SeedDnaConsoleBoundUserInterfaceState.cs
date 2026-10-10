using Content.Shared.ADT.SeedDna.Prototypes;
using Robust.Shared.Serialization;

// ReSharper disable once CheckNamespace
namespace Content.Shared.ADT.SeedDna;

/// <summary>
/// Контейнер для передачи состояния UI между клиентом и сервером
/// </summary>
[Serializable, NetSerializable]
public sealed class SeedDnaConsoleBoundUserInterfaceState(
    bool isSeedsPresent,
    string seedsName,
    bool isDnaDiskPresent,
    string dnaDiskName,
    List<SeedDnaGeneEntry> genes,
    bool connectedToServer,
    int sellPrice,
    bool canSell,
    int points,
    int maxPoints,
    bool seedModified
) : BoundUserInterfaceState
{
    public readonly bool IsSeedsPresent = isSeedsPresent;
    public readonly string SeedsName = seedsName;
    public readonly bool IsDnaDiskPresent = isDnaDiskPresent;
    public readonly string DnaDiskName = dnaDiskName;
    public readonly List<SeedDnaGeneEntry> Genes = genes;
    public readonly bool ConnectedToServer = connectedToServer;
    public readonly int SellPrice = sellPrice;
    public readonly bool CanSell = canSell;
    public readonly int Points = points;
    public readonly int MaxPoints = maxPoints;
    public readonly bool SeedModified = seedModified;
}

[Serializable, NetSerializable]
public sealed class SeedDnaGeneEntry
{
    public const string ChemicalPrefix = "chemical:";
    public const string ConsumeGasPrefix = "consumeGas:";
    public const string ExudeGasPrefix = "exudeGas:";
    public string Id = default!;
    public string Name = default!;
    public string? GasName;

    public SeedDnaGeneType Type;
    public string? SeedValue;
    public string? DiskValue;

    public bool Locked;
    public int Cost;
    public string? LockedTech;
    public string? Description;
}
