using Content.Shared.Destructible.Thresholds;
using Content.Shared.Whitelist;
using Robust.Shared.Prototypes;

namespace Content.Server.ADT.Hallucinations.Types;

public sealed partial class MobHallucinations : BaseHallucinationsType
{
    [DataField]
    public List<EntProtoId> Entities = new();

    [DataField]
    public MinMax Range = new();

    [DataField]
    public MinMax SpawnCount = new();

    [DataField]
    public EntityWhitelist? Whitelist;

    [DataField]
    public EntityWhitelist? Blacklist = new()
    {
        Tags = new(){ "Wall" }
    };
}
