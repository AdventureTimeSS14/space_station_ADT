using Content.Shared.DeviceLinking;
using Robust.Shared.Audio;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Server.ADT.OpenSign;

[RegisterComponent, Access(typeof(OpenSignSystem))]
public sealed partial class OpenSignComponent : Component
{
    [DataField]
    public bool State;

    [DataField("onPort", customTypeSerializer: typeof(PrototypeIdSerializer<SinkPortPrototype>))]
    public string OnPort = "On";

    [DataField("offPort", customTypeSerializer: typeof(PrototypeIdSerializer<SinkPortPrototype>))]
    public string OffPort = "Off";

    [DataField("togglePort", customTypeSerializer: typeof(PrototypeIdSerializer<SinkPortPrototype>))]
    public string TogglePort = "Toggle";

    [DataField("statusPort", customTypeSerializer: typeof(PrototypeIdSerializer<SourcePortPrototype>))]
    public string StatusPort = "Status";

    [DataField]
    public SoundSpecifier ClickSound = new SoundPathSpecifier("/Audio/Machines/lightswitch.ogg");
}
