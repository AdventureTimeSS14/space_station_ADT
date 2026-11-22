using Content.Server.ADT.StationEvents.Events;
using Robust.Shared.Map;

namespace Content.Server.ADT.StationEvents.Components;

[RegisterComponent, Access(typeof(VentSpawnRule))]
public sealed partial class VentSpawnRuleComponent : Component
{
    [DataField]
    public MapCoordinates? Coords;
}
