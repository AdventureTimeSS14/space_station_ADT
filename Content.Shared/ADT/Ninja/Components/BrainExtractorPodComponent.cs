using Content.Shared.DeviceLinking;
using Robust.Shared.Containers;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.ADT.Ninja.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, AutoGenerateComponentPause]
public sealed partial class BrainExtractorPodComponent : Component
{
    [DataField]
    public ProtoId<SinkPortPrototype> PodPort = "BrainExtractorReceiver";

    [ViewVariables]
    public ContainerSlot BodyContainer = default!;

    [DataField, AutoNetworkedField, AutoPausedField]
    public TimeSpan? ScanEndTime;

    [DataField, AutoNetworkedField]
    public bool IsScanning;

    [ViewVariables]
    public EntityUid? ConnectedConsole;

    [ViewVariables]
    public EntityUid? ScanningNinja;

    [DataField]
    public TimeSpan ScanDuration = TimeSpan.FromSeconds(60);

    [DataField]
    public TimeSpan SleepDuration = TimeSpan.FromSeconds(120);

    [DataField]
    public float MaxDistance = 6f;
}

[Serializable, NetSerializable]
public enum BrainExtractorVisuals : byte
{
    Status
}

[Serializable, NetSerializable]
public enum BrainExtractorStatus : byte
{
    Idle,
    Scanning
}
