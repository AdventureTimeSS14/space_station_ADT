using Content.Shared.Access.Components;
using Content.Shared.Storage;
using Content.Shared.Storage.EntitySystems;
using Robust.Shared.Containers;

namespace Content.Shared.ADT.Clothing.Wallet;

/// <summary>
/// Handles wallet access forwarding and wallet appearance updates
/// based on the presence of ID cards inside the wallet.
/// </summary>
public sealed class WalletSystem : EntitySystem
{
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<WalletComponent, GetAdditionalAccessEvent>(OnGetAdditionalAccess);

        SubscribeLocalEvent<WalletComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<WalletComponent, EntInsertedIntoContainerMessage>(OnInserted);
        SubscribeLocalEvent<WalletComponent, EntRemovedFromContainerMessage>(OnRemoved);
    }

    private void OnGetAdditionalAccess(
        EntityUid uid,
        WalletComponent comp,
        ref GetAdditionalAccessEvent args)
    {
        if (!TryComp(uid, out StorageComponent? storage))
            return;

        foreach (var item in storage.StoredItems.Keys)
        {
            args.Entities.Add(item);
        }

    }

    private int CountCards(EntityUid uid)
    {
        var count = 0;

        if (!TryComp(uid, out StorageComponent? storage))
            return 0;

        foreach (var item in storage.StoredItems.Keys)
        {
            if (HasComp<IdCardComponent>(item))
                count++;
        }

        return count;
    }

    private void OnStartup(
        EntityUid uid,
        WalletComponent component,
        ComponentStartup args)
    {
        component.IdCardsInside = CountCards(uid);


        _appearance.SetData(uid,WalletVisuals.StatusWallet, component.IdCardsInside > 0);
    }

    private void OnInserted(
        EntityUid uid,
        WalletComponent component,
        EntInsertedIntoContainerMessage args)
    {
        if (!HasComp<IdCardComponent>(args.Entity))
            return;

        component.IdCardsInside++;

        _appearance.SetData(uid,WalletVisuals.StatusWallet,true);
    }

    private void OnRemoved(
        EntityUid uid,
        WalletComponent component,
        EntRemovedFromContainerMessage args)
    {
        if (!HasComp<IdCardComponent>(args.Entity))
            return;

        component.IdCardsInside--;

        _appearance.SetData(uid, WalletVisuals.StatusWallet, component.IdCardsInside > 0);
    }

}

