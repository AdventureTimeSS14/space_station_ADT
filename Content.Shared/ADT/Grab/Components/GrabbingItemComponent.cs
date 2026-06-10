using Robust.Shared.GameStates;

namespace Content.Shared.ADT.Grab;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class GrabbingItemComponent : Component
{
    [DataField]
    public GrabStage GrabStageOverride = GrabStage.Hard;

    [DataField]
    public float EscapeAttemptModifier = 2f;

    [DataField, AutoNetworkedField]
    public EntityUid? ActivelyGrabbingEntity;
}
