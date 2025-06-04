using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.ADT.HoloCigar.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class HoloCigarComponent : Component
{
    [ViewVariables]
    public bool Lit;

    [ViewVariables]
    public SoundSpecifier SwitchAudio = new SoundPathSpecifier("/Audio/ADT/Items/TheManWhoSoldTheWorld/switch.ogg");
}

[Serializable, NetSerializable]
public sealed class HoloCigarComponentState : ComponentState
{
    public bool Lit;
    public HoloCigarComponentState(bool lit)
    {
        Lit = lit;
    }
}
