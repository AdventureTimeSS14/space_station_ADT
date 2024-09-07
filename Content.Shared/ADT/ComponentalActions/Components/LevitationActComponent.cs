using Robust.Shared.Prototypes;
using Content.Shared.Alert;

namespace Content.Shared.ComponentalActions.Components;

[RegisterComponent]
[AutoGenerateComponentState(true)]
public sealed partial class LevitationActComponent : Component
{
    [DataField]
    public float SpeedModifier = 2f;

    [DataField]
    public float BaseSprintSpeed = 4.5f;

    [DataField]
    public float BaseWalkSpeed = 2.5f;

    [DataField]
    public bool Active = false;

    [DataField("blinkAction")]
    public EntProtoId Action = "CompLevitationAction";

    [DataField, AutoNetworkedField]
    public EntityUid? ActionEntity;

    [DataField]
    [ViewVariables(VVAccess.ReadWrite)]
    public ProtoId<AlertPrototype> Alert = "ADTLevitation";
}
