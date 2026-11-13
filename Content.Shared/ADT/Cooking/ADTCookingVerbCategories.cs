using Content.Shared.Verbs;

namespace Content.Shared.ADT.Cooking;

public static class ADTCookingVerbCategories
{
    public static readonly VerbCategory StoveHeat = new("adt-verb-category-stove-heat", null);

    public static readonly VerbCategory StoveTake = new("adt-verb-category-stove-take", null);

    public static readonly VerbCategory PlateTake = new("adt-verb-category-plate-take", null);
}
