using Content.Shared.FixedPoint;
using Content.Shared.Store;
using Robust.Shared.Prototypes;

namespace Content.Shared.ADT.ManifestListings;

[ByRefEvent]
public record struct PrependObjectivesSummaryTextEvent(string Text = "");

[ByRefEvent]
public readonly record struct ListingPurchasedEvent(
    EntityUid User,
    EntityUid Store,
    ListingData Data,
    IReadOnlyDictionary<ProtoId<CurrencyPrototype>, FixedPoint2> Cost);
