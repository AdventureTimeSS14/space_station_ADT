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

    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public int MinBoltCount = 2;

    /// <summary>
    /// the number of lightning strikes, at the maximum severity of the anomaly
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public int MaxBoltCount = 5;

    /// <summary>
    /// The maximum radius of the passive electrocution effect
    /// scales with stability
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public float MaxElectrocuteRange = 7f;

    /// <summary>
    /// The maximum amount of damage the electrocution can do
    /// scales with severity
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public float MaxElectrocuteDamage = 20f;

    /// <summary>
    /// The maximum amount of time the electrocution lasts
    /// scales with severity
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan MaxElectrocuteDuration = TimeSpan.FromSeconds(8);

    /// <summary>
    /// Radius of objects that will be ignited if flammable.
    /// </summary>
    [DataField]
    public SoundSpecifier IgniteSound = new SoundPathSpecifier("/Audio/Magic/rumble.ogg");

    [ViewVariables(VVAccess.ReadWrite)]
    public float Severity = 0.3f;
    // [DataField("enabled")]
    // public bool Enabled = true;

    /// <summary>
    /// The maximum chance that each second, when in range of the anomaly, you will be electrocuted.
    /// scales with stability
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public float PassiveElectrocutionChance = 0.05f;

    /// <summary>
    /// Used for tracking seconds, so that we can shock people in a non-tick-dependent way.
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan NextSecond = TimeSpan.Zero;

    /// <summary>
    /// Energy consumed from devices by the emp pulse upon going supercritical.
    /// <summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public float EmpEnergyConsumption = 100000f;

    /// <summary>
    /// Duration of devices being disabled by the emp pulse upon going supercritical.
    /// <summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public float EmpDisabledDuration = 60f;
}
