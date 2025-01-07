using Content.Shared.Actions;

namespace Content.Shared.ADT.Events;

public sealed partial class SpawnWallActionEvent : InstantActionEvent
{
    [DataField]
    public int? Cost { get; private set; }
}
