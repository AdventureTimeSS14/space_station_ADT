using Content.Server.ADT.StationEvents.Events;

namespace Content.Server.ADT.StationEvents.Components;

[RegisterComponent, Access(typeof(HeroEvent))]
public sealed partial class HeroEventComponent : Component
{
}