using Content.Shared.Actions;

namespace Content.Shared.ADT.Events;

[DataDefinition]
public sealed partial class EmptyXenoActionEvent : InstantActionEvent
{
    [DataField]
    public int? Cost { get; private set; }
}
