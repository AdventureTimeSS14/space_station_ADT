using Robust.Shared.Audio;
using Robust.Shared.Prototypes;

namespace Content.Shared.ComponentalActions.Components;

[RegisterComponent]
[AutoGenerateComponentState(true)]
public sealed partial class HealActComponent : Component
{
    [DataField("healSound")]
    public SoundSpecifier HealSound = new SoundPathSpecifier("/Audio/Effects/blobattack.ogg");

    /// <summary>
    /// Volume control for the spell.
    /// </summary>
    [DataField("healVolume")]
    public float HealVolume = 1f;

    [DataField]
    public float RegenerateBurnHealAmount = -50f;

    [DataField]
    public float RegenerateBruteHealAmount = -75f;

    [DataField]
    public float RegenerateBloodVolumeHealAmount = 100f;

    [DataField]
    public float RegenerateBleedReduceAmount = -100f;

    [DataField("healAction")]
    public EntProtoId Action = "CompActionHeal";

    [DataField, AutoNetworkedField]
    public EntityUid? ActionEntity;

}
