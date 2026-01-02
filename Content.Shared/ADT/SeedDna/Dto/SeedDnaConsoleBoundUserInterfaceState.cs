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
    SeedDataDto? seedData,
    bool isDnaDiskPresent,
    string dnaDiskName,
    SeedDataDto? dnaDiskData
) : BoundUserInterfaceState
{
    public readonly bool IsSeedsPresent = isSeedsPresent;
    public readonly string SeedsName = seedsName;
    public readonly SeedDataDto? SeedData = seedData;

    public readonly bool IsDnaDiskPresent = isDnaDiskPresent;
    public readonly string DnaDiskName = dnaDiskName;
    public readonly SeedDataDto? DnaDiskData = dnaDiskData;
}
