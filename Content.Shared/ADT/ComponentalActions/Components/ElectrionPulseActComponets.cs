using Robust.Shared.Audio;
using Robust.Shared.Prototypes;


namespace Content.Shared.ComponentalActions.Components;

/// <summary>
/// Lets its owner entity ignite flammables around it and also heal some damage.
/// </summary>
[RegisterComponent]
[AutoGenerateComponentState(true)]
public sealed partial class ElectrionPulseActComponent : Component
{
    [DataField]
    public float IgnitionRadius = 5f;

    /// <summary>
    /// The action entity.
    /// </summary>
    [DataField("blinkAction")]
    public EntProtoId Action = "CompElectrionPulseAction";

    [DataField, AutoNetworkedField]
    public EntityUid? ActionEntity;

    [DataField]
    public SoundSpecifier IgniteSound = new SoundPathSpecifier("/Audio/Magic/forcewall.ogg");

    [DataField("range")]
    [ViewVariables(VVAccess.ReadWrite)]
    public float Range { get; set; } = 7f;
}
