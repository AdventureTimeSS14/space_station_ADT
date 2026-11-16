using Robust.Shared.Maths;

namespace Content.Server.ADT.Weapons.Hitscan.Components;

/// <summary>
/// Spawns a cleanable blood-splatter decal when this hitscan deals damage to something with a bloodstream.
/// </summary>
[RegisterComponent]
public sealed partial class HitscanCreateBloodSprayComponent : Component
{
    /// <summary>
    /// Decals with this tag are picked at random.
    /// </summary>
    [DataField]
    public string DecalTag = "BloodSplatter";

    /// <summary>
    /// How far back along the shot the decal sits, in tiles.
    /// </summary>
    [DataField]
    public float Offset = 0.2f;

    /// <summary>
    /// The splatter art streaks up-left. This is subtracted from the shot angle so the streak follows the bullet.
    /// </summary>
    [DataField]
    public Angle ArtAngle = Angle.FromDegrees(135);

    /// <summary>
    /// Wait before placing the decal, so it appears with the impact.
    /// </summary>
    [DataField]
    public TimeSpan Delay = TimeSpan.FromMilliseconds(200);
}
