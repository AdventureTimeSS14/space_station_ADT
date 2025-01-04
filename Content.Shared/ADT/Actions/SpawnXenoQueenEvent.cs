using Content.Shared.Actions;
using Content.Shared.Storage;
using Content.Shared.Magic;

namespace Content.Shared.ADT.Events;

// TODO: This class needs combining with InstantSpawnSpellEvent
public sealed partial class SpawnXenoQueenEvent : WorldTargetActionEvent, ISpeakSpell 
{
    [DataField]
    public List<EntitySpawnEntry> Prototypes = new();

    [DataField]
    public string? Speech { get; private set; }

    [DataField]
    public int? Cost { get; private set; }
}
