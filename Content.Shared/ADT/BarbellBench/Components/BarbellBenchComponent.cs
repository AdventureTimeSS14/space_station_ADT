using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.ADT.BarbellBench.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class BarbellBenchComponent : Component
{
    [DataField("overlayPrototype"), AutoNetworkedField]
    public EntProtoId OverlayPrototype = "ADTBarbellBenchOverlayVisual";

    [DataField("barbellSlotId")]
    public string BarbellSlotId = "adt-aslot-barbell";

    [DataField("repSoundCollection")]
    public string RepSoundCollection = "BarbellBenchRep";

    [DataField("repSoundDelay")]
    public float RepSoundDelay = 1f;

    [DataField("repSoundMaxDistance")]
    public float RepSoundMaxDistance = 6f;

    [DataField, AutoNetworkedField]
    public bool IsPerformingRep = false;

    [DataField]
    public float RepDuration = 3.0f;

    [AutoNetworkedField]
    public EntityUid? BarbellRepAction;
}
