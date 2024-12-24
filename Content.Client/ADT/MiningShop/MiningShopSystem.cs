using Content.Shared.ADT.MiningShop;

namespace Content.Client.ADT.MiningShop;

public sealed class MiningShopSystem : SharedMiningShopSystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MiningShopComponent, AfterAutoHandleStateEvent>(OnRefresh);
    }

    private void OnRefresh<T>(Entity<T> ent, ref AfterAutoHandleStateEvent args) where T : IComponent?
    {
        if (!TryComp(ent, out UserInterfaceComponent? ui))
            return;

        foreach (var bui in ui.ClientOpenInterfaces.Values)
        {
            if (bui is MiningShopBui vendorUi)
                vendorUi.Refresh();
        }
    }
}
