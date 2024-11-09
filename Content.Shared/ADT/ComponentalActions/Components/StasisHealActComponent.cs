using Robust.Shared.Audio;
using Robust.Shared.Prototypes;

namespace Content.Shared.ComponentalActions.Components;

[RegisterComponent]
[AutoGenerateComponentState(true)]
public sealed partial class StasisHealActComponent : Component
{
    [DataField]
    public bool Active = false;

    [DataField]
    public float SpeedModifier = 1f;

    [DataField]
    public float BaseSprintSpeed = 3f;

    [DataField]
    public float BaseWalkSpeed = 3f;

    [DataField]
    public float RegenerateBurnHealAmount = -0.05f;

    [DataField]
    public float RegenerateBruteHealAmount = -0.1f;

    [DataField]
    public float RegenerateBloodVolumeHealAmount = 0.25f;

    [DataField]
    public float RegenerateBleedReduceAmount = -0.01f;

    [DataField("healAction")]
    public EntProtoId Action = "CompActionStasisHeal";

    [DataField, AutoNetworkedField]
    public EntityUid? ActionEntity;

}
