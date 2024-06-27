using Robust.Shared.Audio;
using Robust.Shared.Prototypes;

namespace Content.Shared.ComponentalActions.Components;

[RegisterComponent]
[AutoGenerateComponentState(true)]
public sealed partial class InvisibilityActComponent : Component
{
    [DataField]
    public bool Active = false;

    [DataField("passiveVisibilityRate")]
    public float PassiveVisibilityRate = -0.10f;

    [DataField("movementVisibilityRate")]
    public float MovementVisibilityRate = 0.10f;

    [DataField("minVisibility")]
    public float MinVisibility = -1f;

    [DataField("maxVisibility")]
    public float MaxVisibility = 1.5f;

    [DataField("stealthAction")]
    public EntProtoId Action = "CompActionStealth";

    [DataField, AutoNetworkedField]
    public EntityUid? ActionEntity;
}
