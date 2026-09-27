namespace Content.Shared.ADT.Lavaland.Components;

[RegisterComponent]
public sealed partial class ADTRuperiumComponent : Component
{
    [DataField]
    public TimeSpan CutDelay = TimeSpan.FromSeconds(5);

    [DataField]
    public bool Shielded = true;
}
