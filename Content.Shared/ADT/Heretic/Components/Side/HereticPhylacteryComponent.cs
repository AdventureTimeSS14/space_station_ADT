using Robust.Shared.Audio;
using Robust.Shared.GameStates;

namespace Content.Shared.ADT.Heretic.Components.Side;

[RegisterComponent, NetworkedComponent]
public sealed partial class HereticPhylacteryComponent : Component
{
    [DataField]
    public string SolutionName = "phylactery";

    [DataField]
    public SoundSpecifier DrawSound = new SoundPathSpecifier("/Audio/Effects/Chemistry/bubbles.ogg");
}
