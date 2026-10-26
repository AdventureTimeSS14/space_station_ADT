using Robust.Shared.Audio;
using Robust.Shared.Prototypes;

namespace Content.Shared.ADT.BorgMarkers;

[RegisterComponent]
public sealed partial class ADTBorgMarkerPlacerComponent : Component
{
    [DataField]
    public EntProtoId MarkerProto = "ADTBorgMarker";

    [DataField]
    public List<ADTBorgMarkerColor> Palette = new();

    [DataField]
    public int SelectedColor;

    [DataField]
    public int MaxMarkers = 8;

    [DataField]
    public float RemoveRange = 0.9f;

    [DataField]
    public SoundSpecifier? SoundOnPlace = new SoundPathSpecifier("/Audio/Machines/quickbeep.ogg")
    {
        Params = AudioParams.Default.WithVolume(-6f).WithMaxDistance(5f),
    };

    [ViewVariables]
    public readonly List<EntityUid> Markers = new();
}

[DataDefinition]
public sealed partial class ADTBorgMarkerColor
{
    [DataField(required: true)]
    public Color Color;

    [DataField(required: true)]
    public LocId Name;
}
