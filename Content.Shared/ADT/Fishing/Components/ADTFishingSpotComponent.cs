using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.ADT.Fishing.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class ADTFishingSpotComponent : Component
{
    [DataField]
    public bool CanBeFished = true;

    [DataField]
    public bool LavalandOnly;

    [DataField]
    public bool RequiresBait = true;

    [DataField]
    public List<EntProtoId> ShoreFish = new();

    [DataField]
    public List<EntProtoId> DeepFish = new();

    [DataField]
    public int ShoreRange = 3;

    [DataField]
    public int KrillFish = 2;

    [DataField]
    public TimeSpan KrillDelay = TimeSpan.FromSeconds(5);

    [ViewVariables]
    public bool? Deep;

    [ViewVariables]
    public bool Occupied;
}
