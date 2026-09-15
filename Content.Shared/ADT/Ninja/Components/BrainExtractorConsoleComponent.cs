using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.ADT.Ninja.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, AutoGenerateComponentPause]
public sealed partial class BrainExtractorConsoleComponent : Component
{
    public const string PodPort = "BrainExtractorSender";

    [ViewVariables]
    public EntityUid? ConnectedPod;

    [DataField]
    public float MaxDistance = 6f;

    [ViewVariables, AutoNetworkedField]
    public bool PodInRange;

    [DataField, AutoNetworkedField, AutoPausedField]
    public TimeSpan? ScanEndTime;

    [DataField, AutoNetworkedField]
    public bool IsScanning;

    [DataField]
    public int MaxScans = 2;
}
