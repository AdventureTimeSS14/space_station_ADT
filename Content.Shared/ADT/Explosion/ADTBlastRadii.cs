using Robust.Shared.Map;
using Robust.Shared.Serialization;

namespace Content.Shared.ADT.Explosion;

[DataDefinition, Serializable, NetSerializable]
public sealed partial class ADTBlastRadii
{
    [DataField]
    public int Devastation;

    [DataField]
    public int Heavy;

    [DataField]
    public int Light;

    public ADTBlastRadii()
    {
    }

    public ADTBlastRadii(int devastation, int heavy, int light)
    {
        Devastation = devastation;
        Heavy = heavy;
        Light = light;
    }

    public int MaxRange => Math.Max(Devastation, Math.Max(Heavy, Light));
}

[Serializable, NetSerializable]
public sealed class ADTBlastEffectEvent : EntityEventArgs
{
    public readonly MapCoordinates Coordinates;
    public readonly ADTBlastRadii Explosion;

    public ADTBlastEffectEvent(MapCoordinates coordinates, ADTBlastRadii explosion)
    {
        Coordinates = coordinates;
        Explosion = explosion;
    }
}
