using Content.Shared.Store;

namespace Content.Shared.ADT.ManifestListings;

[ByRefEvent]
public record struct PrependObjectivesSummaryTextEvent(string Text = "");

[ByRefEvent]
public readonly record struct ListingPurchasedEvent(EntityUid User, EntityUid Store, ListingData Data);
