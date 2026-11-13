using Content.Shared.Humanoid.Markings;
using Content.Shared.Humanoid.Prototypes;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.ADT.Rituals;

[RegisterComponent, NetworkedComponent]
public sealed partial class ADTLavalandDyeComponent : Component
{
    [DataField(required: true)]
    public EntProtoId Mortar;

    [DataField]
    public TimeSpan GrindTime = TimeSpan.FromSeconds(5);
}

[RegisterComponent, NetworkedComponent]
public sealed partial class ADTMortarBowlComponent : Component
{
}

[RegisterComponent, NetworkedComponent]
public sealed partial class ADTMortarComponent : Component
{
    [DataField(required: true)]
    public string Dye = default!;

    [DataField(required: true)]
    public ProtoId<MarkingPrototype> Marking;

    [DataField]
    public int Uses = 5;

    [DataField]
    public EntProtoId Empty = "ADTMushroomBowl";

    [DataField]
    public TimeSpan Delay = TimeSpan.FromSeconds(5);

    [DataField]
    public List<ProtoId<SpeciesPrototype>> Species = new()
    {
        "ADTAshWalkerSpecies",
        "ADTAshWalkerShamanSpecies",
        "ADTDraconidSpecies",
        "Reptilian",
    };
}

[RegisterComponent, NetworkedComponent]
public sealed partial class ADTDyeVisualsComponent : Component
{
    [DataField(required: true)]
    public string Prefix = default!;

    [DataField]
    public string Layer = "dye";
}
