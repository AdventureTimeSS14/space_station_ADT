using Robust.Shared.Map;
using Robust.Shared.Serialization;

namespace Content.Shared.ADT.Weapons.Hitscan.Events;

[Serializable, NetSerializable]
public struct HitscanTrace
{
    public Angle Angle;
    public float Distance;

    public NetCoordinates? MuzzleCoordinates;
    public NetCoordinates? TravelCoordinates;
    public NetCoordinates ImpactCoordinates;
    public NetEntity? ImpactedEnt;
}
