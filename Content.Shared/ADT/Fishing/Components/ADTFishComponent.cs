using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.ADT.Fishing.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class ADTFishComponent : Component
{
    [DataField]
    public EntProtoId? FavoriteBait;

    [DataField]
    public float Difficulty = 0.35f;
}
