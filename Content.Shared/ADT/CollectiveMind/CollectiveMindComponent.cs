using Content.Shared.Radio;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Shared.ADT.CollectiveMind;

[RegisterComponent, NetworkedComponent]
public sealed partial class CollectiveMindRankComponent : Component
{
    [DataField]
    public string RankName = "???";
}
