using Content.Shared.Nutrition.Components;

namespace Content.Server.ADT.NPC.Helpers;

public static class ADTNpcFoodHelper
{
    public static EntityUid? FindFood(IEntityManager ent, EntityUid owner)
    {
        foreach (var item in ADTNpcItemHelper.ReserveItems(ent, owner))
        {
            if (ent.HasComponent<EdibleComponent>(item))
                return item;
        }

        return null;
    }
}
