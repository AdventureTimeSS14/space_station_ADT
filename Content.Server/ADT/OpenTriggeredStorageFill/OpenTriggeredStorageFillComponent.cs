using Content.Shared.Storage;

namespace Content.Server.ADT.OpenTriggeredStorageFill;

/// <summary>
/// This is used for storing an item prototype to be inserted into a container when the trigger is activated. This is deleted from the entity after the item is inserted.
/// </summary>
[RegisterComponent]
public sealed partial class OpenTriggeredStorageFillComponent : Component
{
    [DataField]
    public List<EntitySpawnEntry> Contents = new();
}
