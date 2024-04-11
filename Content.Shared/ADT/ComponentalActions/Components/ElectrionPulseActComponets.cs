using Content.Shared.Atmos.EntitySystems;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;


namespace Content.Shared.ComponentalActions.Components;

/// <summary>
/// Lets its owner entity ignite flammables around it and also heal some damage.
/// </summary>
[RegisterComponent]
[AutoGenerateComponentState(true)]
public sealed partial class ElectrionPulseActComponent : Component
{
    /// <summary>
    /// Radius of objects that will be ignited if flammable.
    /// </summary>
    [DataField]
    public float IgnitionRadius = 4f;

    /// <summary>
    /// The action entity.
    /// </summary>
    [DataField("blinkAction")]
    public EntProtoId Action = "CompElectrionPulseAction";

    [DataField, AutoNetworkedField]
    public EntityUid? ActionEntity;

    [DataField] public EntityUid? ElectrionPulseStarterActionEntity;


    /// <summary>
    /// Radius of objects that will be ignited if flammable.
    /// </summary>
    [DataField]
    public SoundSpecifier IgniteSound = new SoundPathSpecifier("/Audio/Magic/rumble.ogg");


    // [DataField("enabled")]
    // public bool Enabled = true;

    // [DataField("onBump")]
    // public bool OnBump = true;

    // [DataField("onAttacked")]
    // public bool OnAttacked = true;

    // [DataField("noWindowInTile")]
    // public bool NoWindowInTile = false;

    // [DataField("onHandInteract")]
    // public bool OnHandInteract = true;

    // [DataField("onInteractUsing")]
    // public bool OnInteractUsing = true;

    // [DataField("requirePower")]
    // public bool RequirePower = true;

    // [DataField("usesApcPower")]
    // public bool UsesApcPower = false;

    // [DataField("highVoltageNode")]
    // public string? HighVoltageNode;

    // [DataField("mediumVoltageNode")]
    // public string? MediumVoltageNode;

    // [DataField("lowVoltageNode")]
    // public string? LowVoltageNode;

    // [DataField("shockDamage")]
    // public int ShockDamage = 20;

    // /// <summary>
    // ///     Shock time, in seconds.
    // /// </summary>
    // [DataField("shockTime")]
    // public float ShockTime = 8f;

    // [DataField("siemensCoefficient")]
    // public float SiemensCoefficient = 1f;

    // [DataField("shockNoises")]
    // public SoundSpecifier ShockNoises = new SoundCollectionSpecifier("sparks");

    // [DataField("playSoundOnShock")]
    // public bool PlaySoundOnShock = true;

    // [DataField("shockVolume")]
    // public float ShockVolume = 20;
}
