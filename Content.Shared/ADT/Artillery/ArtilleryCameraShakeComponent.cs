using System.Numerics;

namespace Content.Shared.ADT.Artillery;

[RegisterComponent]
public sealed partial class ArtilleryCameraShakeComponent : Component
{
    [DataField]
    public TimeSpan StartTime;

    [DataField]
    public TimeSpan EndTime;

    [DataField]
    public Vector2 EpicenterPosition;

    [DataField]
    public float BaseIntensity;

    [DataField]
    public int ShakeCount;

    [DataField]
    public TimeSpan LastShakeTime;

    [DataField]
    public EntityUid CannonUid;

    [DataField("shakeDuration")]
    public float ShakeDuration = 2f;

    [DataField("shakeIntensity")]
    public float ShakeIntensity = 4.0f;

    [DataField("shakeInterval")]
    public float ShakeInterval = 0.2f;
}
