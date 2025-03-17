using Content.Shared.Actions;
using Content.Shared.Storage;

namespace Content.Shared.ADT.Events;

// TODO: This class needs combining with InstantSpawnSpellEvent
[DataDefinition]
public sealed partial class SpawnXenoQueenEvent : WorldTargetActionEvent
{
    [DataField]
    public List<EntitySpawnEntry> Prototypes = new();

    [DataField]
    public string? Speech { get; private set; }

    [DataField]
    public int? Cost { get; private set; }
}
