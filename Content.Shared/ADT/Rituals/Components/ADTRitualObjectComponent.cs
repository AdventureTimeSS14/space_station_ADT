using Content.Shared.Humanoid.Prototypes;
using Robust.Shared.Prototypes;

namespace Content.Shared.ADT.Rituals;

[RegisterComponent]
public sealed partial class ADTRitualObjectComponent : Component
{
    [DataField(required: true)]
    public List<string> Categories = new();

    [DataField]
    public List<ProtoId<SpeciesPrototype>> AllowedSpecies = new();

    [DataField]
    public Dictionary<ProtoId<ADTRitualPrototype>, int> Charges = new();

    [DataField]
    public Dictionary<ProtoId<ADTRitualPrototype>, TimeSpan> Cooldowns = new();

    [DataField]
    public EntProtoId? FinaleEffect = "ADTAshRuneActivation";

    [DataField]
    public TimeSpan FinaleDelay = TimeSpan.FromSeconds(3);

    [ViewVariables]
    public bool Busy;
}
