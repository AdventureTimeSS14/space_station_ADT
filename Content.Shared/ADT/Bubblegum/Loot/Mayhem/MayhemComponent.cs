using Robust.Shared.Audio;
using Robust.Shared.GameStates;

namespace Content.Shared.ADT.Bubblegum.Loot;

[RegisterComponent, NetworkedComponent]
public sealed partial class MayhemComponent : Component
{
    [DataField]
    public float Range = 7f;

    [DataField]
    public TimeSpan EffectDelay = TimeSpan.FromSeconds(15);

    [DataField]
    public SoundSpecifier BreakSound = new SoundPathSpecifier("/Audio/ADT/Bubblegum/glassbr1.ogg");
}
