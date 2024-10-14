using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.ADT.Shadekin.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class ShadekinComponent : Component
{
    #region Random occurrences
    /// <summary>
    /// Accumulator that indicates how long shadekin were with max energy level
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    public float MaxedPowerAccumulator = 0f;

    /// <summary>
    /// Teleport randomly if MaxedPowerAccumulator is greater
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    public float MaxedPowerRoof = 60f;

    /// <summary>
    /// Accumulator that indicates how long shadekin were with min energy level
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    public float MinPowerAccumulator = 0f;

    /// <summary>
    /// Blackeye if MinPowerAccumulator is greater
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    public float MinPowerRoof = 30f;
    #endregion


    #region Shader
    /// <summary>
    ///     Automatically set to eye color.
    /// </summary>
    [ViewVariables(VVAccess.ReadOnly)]
    public Vector3 TintColor = new(0.5f, 0f, 0.5f);

    /// <summary>
    ///     Based on PowerLevel.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    public float TintIntensity = 0.65f;
    #endregion


    #region Power level
    /// <summary>
    ///     Current amount of energy.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), AutoNetworkedField]
    public float PowerLevel
    {
        get => _powerLevel;
        set => _powerLevel = Math.Clamp(value, PowerThresholds[ShadekinPowerThreshold.Min], PowerThresholds[ShadekinPowerThreshold.Max]);
    }
    public float _powerLevel = 150f;

    /// <summary>
    ///     Don't let PowerLevel go above this value.
    /// </summary>
    [ViewVariables(VVAccess.ReadOnly), AutoNetworkedField]
    public float PowerLevelMax = PowerThresholds[ShadekinPowerThreshold.Max];

    /// <summary>
    ///     Blackeye chance if PowerLevel less than this value.
    /// </summary>
    [ViewVariables(VVAccess.ReadOnly), AutoNetworkedField]
    public float PowerLevelMin = PowerThresholds[ShadekinPowerThreshold.Tired];

    /// <summary>
    ///     How much energy is gained per second.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), AutoNetworkedField]
    public float PowerLevelGain = 0.5f;

    /// <summary>
    ///     Power gain multiplier
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), AutoNetworkedField]
    public float PowerLevelGainMultiplier = 1f;

    /// <summary>
    ///     Whether to gain power or not.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), AutoNetworkedField]
    public bool PowerLevelGainEnabled = true;

    /// <summary>
    ///     Whether they are a blackeye.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), AutoNetworkedField]
    public bool Blackeye = false;

    [ViewVariables(VVAccess.ReadWrite), AutoNetworkedField]
    public bool RoundstartBlackeyeChecked = false;

    /// <summary>
    /// Next second to gain energy
    /// </summary>
    [ViewVariables(VVAccess.ReadOnly), AutoNetworkedField]
    public TimeSpan NextSecond = TimeSpan.Zero;


    public static Dictionary<ShadekinPowerThreshold, float> PowerThresholds = new()
    {
        { ShadekinPowerThreshold.Max, 250.0f },
        { ShadekinPowerThreshold.Great, 200.0f },
        { ShadekinPowerThreshold.Good, 150.0f },
        { ShadekinPowerThreshold.Okay, 100.0f },
        { ShadekinPowerThreshold.Tired, 50.0f },
        { ShadekinPowerThreshold.Min, 0.0f },
    };
    #endregion

    #region Actions
    public string ActionProto = "ActionShadekinTeleport";
    public EntityUid? ActionEntity;
    #endregion
}

public enum ShadekinPowerThreshold : byte
{
    Max = 1 << 4,
    Great = 1 << 3,
    Good = 1 << 2,
    Okay = 1 << 1,
    Tired = 1 << 0,
    Min = 0,
}
