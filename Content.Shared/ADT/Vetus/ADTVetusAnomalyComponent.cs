using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared.ADT.Vetus;

[RegisterComponent, AutoGenerateComponentPause]
public sealed partial class ADTVetusAnomalyComponent : Component
{
    [DataField]
    public ADTVetusMode Mode;

    [DataField]
    public TimeSpan ProcessInterval = TimeSpan.FromSeconds(2);

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoPausedField]
    public TimeSpan NextProcess;

    [DataField]
    public float MoveChance = 0.7f;

    [ViewVariables]
    public bool Charged;

    [ViewVariables]
    public int Ticks;

    [DataField]
    public int PullRange = 4;

    [DataField]
    public float GravThrowDistance = 5f;

    [DataField]
    public float GravThrowSpeed = 5f;

    [DataField]
    public int ShockDamage = 20;

    [DataField]
    public int ProcessTeleportRange = 4;

    [DataField]
    public int BumpTeleportRange = 8;

    [DataField]
    public int GasTicks = 5;

    [DataField]
    public float GasMoles = 5f;

    [DataField]
    public float GasTemperature = 1000f;

    [DataField]
    public int VortexMaxRadius = 3;

    [DataField]
    public float VortexPullChance = 0.5f;

    [DataField]
    public float VortexTurfChance = 0.25f;
}
