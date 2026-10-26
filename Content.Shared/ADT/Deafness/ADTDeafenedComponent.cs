using Robust.Shared.GameStates;

namespace Content.Shared.ADT.Deafness;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class ADTDeafenedComponent : Component
{
    [DataField, AutoNetworkedField]
    public float Severity;

    [DataField, AutoNetworkedField]
    public float TotalThreshold = 2f;

    [DataField]
    public float DecayPerSecond = 0.5f;

    [DataField]
    public LocId TotalMessage = "adt-deafness-total";

    [DataField]
    public LocId PartialMessage = "adt-deafness-partial";

    [DataField]
    public LocId RecoveryMessage = "adt-deafness-recovered";
}
