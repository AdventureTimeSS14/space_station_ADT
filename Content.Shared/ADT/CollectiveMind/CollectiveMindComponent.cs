using Content.Shared.Radio;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Shared.Sirena.CollectiveMind;

[RegisterComponent, NetworkedComponent]
public sealed partial class CollectiveMindComponent : Component
{
    [DataField(required: true, customTypeSerializer: typeof(PrototypeIdSerializer<RadioChannelPrototype>))]
    public string Channel = string.Empty;

    [DataField]
    public bool ShowName = false;

    [DataField]
    public bool ShowRank = false;

    [DataField]
    public string RankName = "???";
}
