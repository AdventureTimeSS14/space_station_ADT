using Content.Server.StationEvents.Events;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Server.StationEvents.Components;

[RegisterComponent, Access(typeof(TraderSpawnRule))]
public sealed partial class TraderSpawnRuleComponent : Component
{
    [DataField("TraderShuttlePath")]
    public string TraderShuttlePath = "Maps/traderpost.yml";
}
