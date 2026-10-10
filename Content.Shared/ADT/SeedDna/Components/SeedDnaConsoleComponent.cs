using Content.Shared.ADT.SeedDna.Prototypes;
using Content.Shared.Cargo.Prototypes;
using Content.Shared.Containers.ItemSlots;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.ADT.SeedDna.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class SeedDnaConsoleComponent : Component
{
    public static string SeedSlotId = "SeedSlotId";
    public static string DnaDiskSlotId = "DnaDiskSlotId";

    [DataField]
    public ItemSlot SeedSlot = new();

    [DataField]
    public ItemSlot DnaDiskSlot = new();

    [DataField]
    public List<ProtoId<SeedDnaGenePrototype>> Genes = new();

    [DataField]
    public float MaxChemicalsVolume = 100f;

    [DataField]
    public TimeSpan TransferCooldown = TimeSpan.FromSeconds(5f);

    [DataField]
    public int SellBasePrice = 200;

    [DataField]
    public int SellChemicalPrice = 25;

    [DataField]
    public ProtoId<CargoAccountPrototype> SellAccount = "Service";

    [DataField]
    public TimeSpan SellCooldown = TimeSpan.FromSeconds(30f);

    [DataField]
    public float SellPointsPercent = 10f;

    [DataField]
    public int MaxPoints = 100;

    [DataField]
    public int StartingPoints = 50;

    [DataField]
    public bool RequireModifiedSeed = true;

    [ViewVariables] public TimeSpan NextTransferAt;
    [ViewVariables] public TimeSpan NextSellAt;
    [ViewVariables] public int Points;
}
