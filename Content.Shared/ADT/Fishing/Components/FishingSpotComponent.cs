using Content.Shared.EntityTable.EntitySelectors;

namespace Content.Shared.ADT.Fishing.Components;

[RegisterComponent]
public sealed partial class FishingSpotComponent : Component
{
    [DataField(required: true)]
    public EntityTableSelector FishList;

    [DataField]
    public float FishDefaultTimer;

    [DataField]
    public float FishTimerVariety;
}
