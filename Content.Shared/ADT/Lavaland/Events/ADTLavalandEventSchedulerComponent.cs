using Content.Shared.Destructible.Thresholds;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared.ADT.Lavaland.Events;

[RegisterComponent, AutoGenerateComponentPause]
public sealed partial class ADTLavalandEventSchedulerComponent : Component
{
    [DataField(required: true)]
    public List<ProtoId<ADTLavalandEventPrototype>> Events = new();

    [DataField]
    public MinMax Delay = new(900, 1800);

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoPausedField]
    public TimeSpan NextEvent;
}
