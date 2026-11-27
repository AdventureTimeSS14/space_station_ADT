using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared.ADT.IPC;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, AutoGenerateComponentPause]
public sealed partial class SiliconComponent : Component
{
    [DataField]
    public bool BatteryPowered;

    [DataField]
    public float DrainPerSecond = 50f;

    [DataField]
    public float IdleDrainReduction = 0.6f;

    [DataField(required: true)]
    public Dictionary<int, float> SpeedModifierThresholds = new();

    [DataField]
    public float FireStackMultiplier = 1f;

    [DataField]
    public bool CanSleep;

    [DataField]
    public bool LockBatterySlot;

    [DataField]
    public bool DisableLightOnDeath;

    [DataField]
    public bool DownOnDrained;

    [DataField]
    public SoundSpecifier? DrainedSound;

    [DataField]
    public TimeSpan DrainedSoundMinInterval = TimeSpan.FromSeconds(8);

    [DataField]
    public TimeSpan DrainedSoundMaxInterval = TimeSpan.FromSeconds(15);

    [DataField]
    public LocId? DrainedPopup = "silicon-power-low";

    [DataField]
    public TimeSpan UpdateInterval = TimeSpan.FromSeconds(1);

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoPausedField]
    public TimeSpan NextUpdate;

    [DataField, AutoNetworkedField]
    public short ChargeState = 10;

    [DataField, AutoNetworkedField]
    public bool Drained;

    [DataField]
    public bool LightDisabledByDeath;

    [DataField]
    public float OverheatAccumulator;
}
