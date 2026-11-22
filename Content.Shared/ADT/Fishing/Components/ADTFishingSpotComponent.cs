using Content.Shared.EntityTable.EntitySelectors;
using Content.Shared.Maps;
using Content.Shared.Whitelist;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.ADT.Fishing.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class ADTFishingSpotComponent : Component
{
    [DataField]
    public bool CanBeFished = true;

    [DataField]
    public bool LavalandOnly;

    [DataField]
    public bool RequiresBait = true;

    [DataField]
    public List<EntProtoId> ShoreFish = new();

    [DataField]
    public List<EntProtoId> DeepFish = new();

    [DataField]
    public EntityTableSelector? Junk;

    [DataField]
    public float JunkChance;

    [DataField]
    public int ShoreRange = 3;

    [DataField]
    public List<ProtoId<ContentTileDefinition>> ShoreTiles = new();

    [DataField]
    public EntityWhitelist? ShoreBlacklist;

    [DataField]
    public int KrillFish = 2;

    [DataField]
    public TimeSpan KrillDelay = TimeSpan.FromSeconds(5);

    [ViewVariables, AutoNetworkedField]
    public bool? Deep;

    [ViewVariables]
    public bool Occupied;
}
