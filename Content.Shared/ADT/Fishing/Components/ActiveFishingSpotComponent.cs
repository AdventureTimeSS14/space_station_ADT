using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.ADT.Fishing.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class ActiveFishingSpotComponent : Component
{
    [ViewVariables, AutoNetworkedField]
    public EntityUid? AttachedFishingLure;

    [DataField, AutoNetworkedField]
    public TimeSpan? FishingStartTime;

    [DataField, AutoNetworkedField]
    public bool IsActive;

    [DataField, AutoNetworkedField]
    public float FishDifficulty;

    [DataField]
    public EntProtoId? Fish;
}
