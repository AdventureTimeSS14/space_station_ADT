using Content.Shared.DeviceLinking;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;

namespace Content.Server.ADT.OpenSign;

[RegisterComponent, Access(typeof(OpenSignSystem))]
public sealed partial class OpenSignComponent : Component
{
    [DataField]
    public bool State;

    [DataField("onPort")]
    public ProtoId<SinkPortPrototype> OnPort = "On";

    [DataField("offPort")]
    public ProtoId<SinkPortPrototype> OffPort = "Off";

    [DataField("togglePort")]
    public ProtoId<SinkPortPrototype> TogglePort = "Toggle";

    [DataField("statusPort")]
    public ProtoId<SourcePortPrototype> StatusPort = "Status";

    [DataField]
    public SoundSpecifier ClickSound = new SoundPathSpecifier("/Audio/Machines/lightswitch.ogg");
}
