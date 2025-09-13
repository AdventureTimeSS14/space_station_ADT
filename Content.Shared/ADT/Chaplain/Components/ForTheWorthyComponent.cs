using Robust.Shared.Audio;
using Robust.Shared.GameStates;

namespace Content.Shared.ADT.Chaplain.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class ForTheWorthyComponent : Component
{
    [DataField("electrocutionSound")]
    public SoundSpecifier ElectrocutionSound = new SoundPathSpecifier("/Audio/Effects/lightburn.ogg");
}
