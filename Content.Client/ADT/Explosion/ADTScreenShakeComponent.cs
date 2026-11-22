using System.Numerics;

namespace Content.Client.ADT.Explosion;

[RegisterComponent]
public sealed partial class ADTScreenShakeComponent : Component
{
    [ViewVariables]
    public List<(TimeSpan Time, Vector2 Offset)> Keys = new();

    [ViewVariables]
    public Vector2 Current;
}
