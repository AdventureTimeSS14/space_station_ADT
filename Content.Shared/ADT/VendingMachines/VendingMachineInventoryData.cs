using Robust.Shared.Prototypes;

namespace Content.Shared.ADT.VendingMachines;

public sealed class VendingMachineItemData
{
    public uint Amount = 1;

    public int Price;
}

public sealed class VendingMachineInventoryData
{
    public uint Amount = 1;

    public Dictionary<string, VendingMachineItemData>? Items;
    public static IEnumerable<(string Id, uint Amount, int Price, string? Category)> Flatten(Dictionary<string, VendingMachineInventoryData> inventory)
    {
        foreach (var (key, data) in inventory)
        {
            if (data.Items == null)
            {
                yield return (key, data.Amount, 0, null);
                continue;
            }

            foreach (var (item, itemData) in data.Items)
                yield return (item, itemData.Amount, itemData.Price, key);
        }
    }
    public static IEnumerable<(string Id, uint Amount, int Price, string? Category)> Flatten(Dictionary<EntProtoId, uint>? inventory)
    {
        if (inventory == null)
            yield break;

        foreach (var (id, amount) in inventory)
            yield return (id, amount, 0, null);
    }

    public static IEnumerable<(string Id, uint Amount, int Price, string? Category)> Flatten(Dictionary<string, uint>? inventory)
    {
        if (inventory == null)
            yield break;

        foreach (var (id, amount) in inventory)
            yield return (id, amount, 0, null);
    }
}
