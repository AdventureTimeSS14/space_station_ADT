using Content.Shared.FixedPoint;
using Content.Shared.Store;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Shared.ADT.ManifestListings;

[RegisterComponent, NetworkedComponent]
public sealed partial class MindListingsComponent : Component
{
    [DataField]
    public Dictionary<int, List<ManifestPurchaseRecord>> Listings = new();

    [DataField]
    public SpriteSpecifier.Texture DefaultTexture = new(new ResPath("/Textures/Interface/Actions/shop.png"));
}

[DataDefinition]
public sealed partial class ManifestPurchaseRecord
{
    [DataField]
    public ListingData Data = default!;

    [DataField]
    public int Amount;

    [DataField]
    public Dictionary<ProtoId<CurrencyPrototype>, FixedPoint2> Spent = new();
}
