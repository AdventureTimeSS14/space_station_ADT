namespace Content.Shared.ADT.Crushers.Components;

[RegisterComponent]
public sealed partial class ADTCrusherStaggerStacksComponent : Component
{
    [ViewVariables]
    public int Stacks;

    [ViewVariables]
    public TimeSpan LastHit;
}
