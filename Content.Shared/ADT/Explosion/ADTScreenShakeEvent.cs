using Robust.Shared.Map;
using Robust.Shared.Serialization;

namespace Content.Shared.ADT.Explosion;

[Serializable, NetSerializable]
public sealed class ADTScreenShakeEvent : EntityEventArgs
{
    public MapCoordinates Center;
    public float Range;
    public float Duration;
    public float Strength;

    public ADTScreenShakeEvent(MapCoordinates center, float range, float duration, float strength)
    {
        Center = center;
        Range = range;
        Duration = duration;
        Strength = strength;
    }
}
