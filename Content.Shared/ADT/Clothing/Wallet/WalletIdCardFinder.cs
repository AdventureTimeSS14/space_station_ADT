using System.Linq;
using Content.Shared.Access.Components;
using Content.Shared.Storage;
using Robust.Shared.GameObjects;

namespace Content.Shared.ADT.Clothing.Wallet;

/// <summary>
/// Поиск ID карты в контейнере через сортировку по x y
/// </summary>
public static class WalletIdCardFinder
{
    public static bool TryGetFirstIdCard(
        IEntityManager entMan,
        StorageComponent storage,
        out Entity<IdCardComponent> idCard)
    {
        foreach (var entity in storage.StoredItems
                     .OrderBy(x => x.Value.Position.X)
                     .ThenBy(x => x.Value.Position.Y)
                     .Select(x => x.Key))
        {
            if (entMan.TryGetComponent(entity, out IdCardComponent? card))
            {
                idCard = (entity, card);
                return true;
            }
        }

        idCard = default;
        return false;
    }
}
