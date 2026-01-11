using Robust.Shared.GameStates;

namespace Content.Shared.ADT.BarbellBench.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class BarbellBenchComponent : Component
{
    [DataField("overlayPrototype")]
    public string OverlayPrototype = "ADTBarbellBenchOverlay";

    [DataField("barbellSlotId")]
    public string BarbellSlotId = "adt-aslot-barbell";

    [DataField("repSoundCollection")]
    public string RepSoundCollection = "BarbellBenchRep";

    [DataField("repSoundDelay")]
    public float RepSoundDelay = 1f;

    [DataField, AutoNetworkedField]
    public bool IsPerformingRep = false;

    [DataField]
    public float RepDuration = 3.0f;

    [DataField, AutoNetworkedField]
    public EntityUid? BarbellRepAction;

    [DataField, AutoNetworkedField]
    public EntityUid? OverlayEntity;
}
