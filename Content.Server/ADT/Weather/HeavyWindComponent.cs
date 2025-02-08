using System.Numerics;
using Robust.Shared.GameStates;

namespace Content.Server.ADT.Weather;

/// <summary>
/// Makes an entity not take damage from ash storms.
/// </summary>
[RegisterComponent]
public sealed partial class HeavyWindComponent : Component
{
    [ViewVariables(VVAccess.ReadWrite)]
    public Vector2 Direction = Vector2.Zero;

    [ViewVariables(VVAccess.ReadWrite)]
    public float Speed = 2f;
}
