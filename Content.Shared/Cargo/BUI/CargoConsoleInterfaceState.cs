using Content.Shared.Cargo.Prototypes;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.Cargo.BUI;

[NetSerializable, Serializable]
public sealed class CargoConsoleInterfaceState : BoundUserInterfaceState
{
    public string Name;
    public int Count;
    public int Capacity;
    public NetEntity Station;
    public List<CargoOrderData> Orders;
    public List<ProtoId<CargoProductPrototype>> Products;

    public Dictionary<ProtoId<CargoProductPrototype>, int> ProductCosts = new(); // ADT-Tweak

    public CargoConsoleInterfaceState(string name, int count, int capacity, NetEntity station, List<CargoOrderData> orders, List<ProtoId<CargoProductPrototype>> products, Dictionary<ProtoId<CargoProductPrototype>, int>? productCosts = null) // ADT-Tweak
    {
        Name = name;
        Count = count;
        Capacity = capacity;
        Station = station;
        Orders = orders;
        Products = products;

        // ADT-Tweak start
        if (productCosts != null)
            ProductCosts = productCosts;
        // ADT-Tweak end
    }
}
