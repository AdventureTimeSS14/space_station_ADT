namespace Content.Shared.ADT.Cooking.Components;

[RegisterComponent]
public sealed partial class ADTActiveCookingComponent : Component
{
    [ViewVariables]
    public EntityUid Stove;

    [ViewVariables]
    public int Slot;

    [ViewVariables]
    public TimeSpan NextUpdate;
}
