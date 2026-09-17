namespace Content.Shared.ADT.VendingMachines;
public sealed class VendingMachineInventoryData
{
    public uint Amount = 1;

    public Dictionary<string, uint>? Items;
    public static IEnumerable<(string Id, uint Amount, string? Category)> Flatten(Dictionary<string, VendingMachineInventoryData> inventory)
    {
        foreach (var (key, data) in inventory)
        {
            if (data.Items == null)
            {
                yield return (key, data.Amount, null);
                continue;
            }

            foreach (var (item, amount) in data.Items)
                yield return (item, amount, key);
        }
    }
    public static IEnumerable<(string Id, uint Amount, string? Category)> Flatten(Dictionary<string, uint>? inventory)
    {
        if (inventory == null)
            yield break;

        foreach (var (id, amount) in inventory)
            yield return (id, amount, null);
    }
}
