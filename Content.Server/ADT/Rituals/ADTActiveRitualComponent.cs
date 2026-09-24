using Content.Shared.ADT.Rituals;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;

namespace Content.Server.ADT.Rituals;

[RegisterComponent]
public sealed partial class ADTActiveRitualComponent : Component
{
    public ProtoId<ADTRitualPrototype> Ritual;

    public EntityUid Invoker;

    public List<EntityUid> Invokers = new();

    public List<EntityUid> UsedThings = new();

    public List<EntityUid> Consumable = new();

    public float DisasterChance;

    public Dictionary<EntityUid, EntityCoordinates> ThingPositions = new();

    public List<EntityUid> Queue = new();

    public int Index;

    public TimeSpan? ResolveAt;
}
// todo add vv or datafields