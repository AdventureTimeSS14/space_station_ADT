using Robust.Shared.GameStates;

namespace Content.Shared.ADT.Sandevistan;

[RegisterComponent]
[NetworkedComponent]
public sealed partial class SandevistanImplantComponent : Component
{
    [DataField("marking")]
    public string? MarkingId = "ADTSandevistan";

    [DataField("markingColor")]
    public Color? MarkingColor = null;

    [DataField("forcedMarking")]
    public bool ForcedMarking = true;
}
