/* (Откат PR - https://github.com/space-wizards/space-station-14/pull/32423)
using Content.Shared.DeviceLinking;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Server.Explosion.Components
{
    /// <summary>
    /// Sends a trigger when signal is received.
    /// </summary>
    [RegisterComponent]
    public sealed partial class TimerStartOnSignalComponent : Component
    {
        [DataField("port", customTypeSerializer: typeof(PrototypeIdSerializer<SinkPortPrototype>))]
        public string Port = "Timer";
    }
}
То же самое, что и с мини бомбой синдиката, но для С4. Чтобы не бахало быстро. 
*/