using Content.Shared.Actions;

namespace Content.Shared.ADT.Mime;

public sealed partial class MimeSilenceActionEvent : InstantActionEvent
{
    [DataField] public float Range = 5f;
    [DataField] public float MuteDuration = 10f;
}
