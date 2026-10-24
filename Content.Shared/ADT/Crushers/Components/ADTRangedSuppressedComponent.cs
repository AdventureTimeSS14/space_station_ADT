namespace Content.Shared.ADT.Crushers.Components;

[RegisterComponent]
public sealed partial class ADTRangedSuppressedComponent : Component
{
    [ViewVariables]
    public TimeSpan Until;
}
