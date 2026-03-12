namespace Content.Shared.ADT.Chalkboard;

[RegisterComponent]
public sealed partial class ChalkboardComponent : Component
{
    [DataField]
    public string? BaseDescription;

    [DataField]
    public int MaxDescriptionLength = 512;
}

