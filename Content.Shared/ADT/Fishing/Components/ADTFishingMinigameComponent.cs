using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.ADT.Fishing.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class ADTFishingMinigameComponent : Component
{
    [DataField, AutoNetworkedField]
    public float FishPosition = 0.5f;

    [DataField, AutoNetworkedField]
    public float HookPosition = 0.5f;

    [DataField, AutoNetworkedField]
    public float HookSize = 0.22f;

    [DataField, AutoNetworkedField]
    public float Progress = 0.4f;

    [DataField, AutoNetworkedField]
    public bool Holding;

    [DataField, AutoNetworkedField]
    public bool Hooked;

    [ViewVariables]
    public float FishTarget = 0.5f;

    [ViewVariables]
    public TimeSpan NextFishMove;

    [ViewVariables]
    public float HookVelocity;

    [ViewVariables]
    public float Difficulty = 0.35f;

    [ViewVariables]
    public float Efficiency = 1f;

    [ViewVariables]
    public float BreakDistance = 4f;

    [ViewVariables, AutoNetworkedField]
    public EntProtoId Fish;

    [ViewVariables]
    public EntityUid User;

    [ViewVariables]
    public EntityUid Spot;
}
