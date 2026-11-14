using Content.Shared.Humanoid;
using Content.Shared.Humanoid.Markings;
using Robust.Shared.Prototypes;

namespace Content.Shared.ADT.Virology.Effects;

public sealed partial class ADTSymptomSetMarking : ADTSymptomEffect<ADTSymptomSetMarking>
{
    [DataField]
    public HumanoidVisualLayers Layer = HumanoidVisualLayers.FacialHair;

    [DataField(required: true)]
    public ProtoId<MarkingPrototype> Marking;

    [DataField]
    public List<ProtoId<MarkingPrototype>> Keep = new();
}

public sealed partial class ADTSymptomClearMarkings : ADTSymptomEffect<ADTSymptomClearMarkings>
{
    [DataField]
    public List<HumanoidVisualLayers> Layers = new() { HumanoidVisualLayers.Hair };
}

public sealed partial class ADTSymptomSkinColor : ADTSymptomEffect<ADTSymptomSkinColor>
{
    [DataField(required: true)]
    public Color Color;
}

public sealed partial class ADTSymptomYouth : ADTSymptomEffect<ADTSymptomYouth>
{
    [DataField]
    public int MinAge = 21;

    [DataField(required: true)]
    public int TargetAge;
}

public sealed partial class ADTSymptomVoiceChange : ADTSymptomEffect<ADTSymptomVoiceChange>
{
}
