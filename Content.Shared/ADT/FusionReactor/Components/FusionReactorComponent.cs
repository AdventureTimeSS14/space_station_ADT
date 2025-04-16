using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;
using Content.Shared.Atmos;

namespace Content.Server.FusionReactor.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class FusioneReactorComponent : Component
{
    [DataField("gasMixture")]
    public GasMixture Gas = new(0f, 2000f);

    [DataField("temperature")]
    public float Temperature => Gas.Temperature;

    [DataField("pressure")]
    public float Pressure => Gas.Pressure;

    [DataField("active")]
    public bool Active = false;

    [DataField("powerOutput")]
    public float PowerOutput = 0f;

    [DataField("coolerUid", customTypeSerializer: typeof(EntityUidSerializer))]
    public EntityUid? CoolerUid = null;

    [DataField("efficiency")]
    public float HeatTransferEfficiency = 0.5f;

    [DataField("coreUid")]
    public EntityUid? CoreUid = null;

    [DataField("sourceCore")]
    public EntityUid? SourceCore = null;
}