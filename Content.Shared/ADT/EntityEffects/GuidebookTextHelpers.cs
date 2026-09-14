using Content.Shared.Chemistry.Reagent;
using Robust.Shared.Prototypes;

namespace Content.Shared.ADT.EntityEffects;
public static class GuidebookTextHelpers
{
    public static string LocalizedEntityName(IPrototypeManager proto, string id)
    {
        if (!proto.TryIndex<EntityPrototype>(id, out var entity))
            return id;

        return Loc.TryGetString($"ent-{id}", out var name) ? name : entity.Name;
    }

    public static string LocalizedReagentName(IPrototypeManager proto, string id)
    {
        return proto.TryIndex<ReagentPrototype>(id, out var reagent) ? reagent.LocalizedName : id;
    }
}