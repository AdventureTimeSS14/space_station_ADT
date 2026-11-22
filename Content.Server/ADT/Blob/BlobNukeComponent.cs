namespace Content.Server.ADT.Blob;

[RegisterComponent]
public sealed partial class BlobNukeComponent : Component
{
    [ViewVariables]
    public float Timer;

    [ViewVariables]
    public TimeSpan NextTileCheck;
}
