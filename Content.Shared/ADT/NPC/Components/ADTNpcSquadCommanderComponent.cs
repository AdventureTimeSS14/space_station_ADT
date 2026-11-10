using Content.Shared.Dataset;
using Content.Shared.NPC.Prototypes;
using Content.Shared.Radio;
using Robust.Shared.Prototypes;

namespace Content.Shared.ADT.NPC.Components;

[RegisterComponent]
public sealed partial class ADTNpcSquadCommanderComponent : Component
{
    [DataField(required: true)]
    public ProtoId<NpcFactionPrototype> Faction;

    [DataField]
    public ProtoId<RadioChannelPrototype>? Channel;

    [DataField]
    public Dictionary<ADTSquadOrder, ProtoId<LocalizedDatasetPrototype>> Lines = new();

    [DataField]
    public Dictionary<ADTSquadOrder, EntProtoId> Actions = new();

    [ViewVariables]
    public Dictionary<ADTSquadOrder, EntityUid?> ActionEntities = new();

    [ViewVariables]
    public bool Follow;

    [ViewVariables]
    public bool Passive;
}
