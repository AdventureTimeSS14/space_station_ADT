using Content.Server.StationEvents.Events;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;
using Content.Server.ADT.StationEvents.Events;

namespace Content.Server.ADT.StationEvents.Components;

[RegisterComponent, Access(typeof(IanHeroEvent))]
public sealed partial class IanHeroEventComponent : Component
{  
}