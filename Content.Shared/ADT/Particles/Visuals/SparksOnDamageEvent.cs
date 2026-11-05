using Robust.Shared.Map;
using Robust.Shared.Serialization;

namespace Content.Shared.ADT.Particles.Visuals;

/// <summary>
/// Tells nearby clients to render a machine spark burst authorized by the server.
/// </summary>
[Serializable, NetSerializable]
public sealed class SparksOnDamageEvent(
    MapCoordinates coordinates,
    string effect,
    Color lightColor,
    float lightRadius,
    float lightEnergy,
    float lightDuration) : EntityEventArgs
{
    public MapCoordinates Coordinates = coordinates;
    public string Effect = effect;
    public Color LightColor = lightColor;
    public float LightRadius = lightRadius;
    public float LightEnergy = lightEnergy;
    public float LightDuration = lightDuration;
}
