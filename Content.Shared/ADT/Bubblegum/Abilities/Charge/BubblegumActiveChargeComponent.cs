using System.Numerics;
using Robust.Shared.GameStates;
using Robust.Shared.Map;

namespace Content.Shared.ADT.Bubblegum;

[RegisterComponent, NetworkedComponent]
public sealed partial class BubblegumActiveChargeComponent : Component
{
    [DataField]
    public TimeSpan EndsAt;

    [DataField]
    public MapCoordinates TargetCoords;

    [DataField]
    public Vector2 Direction;

    [DataField]
    public float TrampleDamage = 30f;

    [DataField]
    public bool ExpireOnHit;

    [DataField]
    public float SmashBlunt = 200f;

    [DataField]
    public float SmashStructural = 300f;

    [DataField]
    public float CameraKickStrength = 0.6f;

    [DataField]
    public HashSet<EntityUid> AlreadySmashed = [];
}
