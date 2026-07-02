using Content.Shared.ADT.Crushers.Systems;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;

namespace Content.Shared.ADT.Crushers.Components;

[RegisterComponent, NetworkedComponent, Access(typeof(TrophyHolderSystem))]
public sealed partial class TrophyHolderComponent : Component
{
    [DataField]
    public string TrophyContainerId = "upgrades";

    [DataField]
    public SoundSpecifier? InsertSound = new SoundPathSpecifier("/Audio/Effects/thunk.ogg");

    [DataField]
    public string ToolQuality = "Prying";
}
