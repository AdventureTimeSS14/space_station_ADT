using Robust.Shared.Audio;

namespace Content.Shared.ADT.TheManWhoSoldTheWorld.Components;

[RegisterComponent]
public sealed partial class TheManWhoSoldTheWorldComponent : Component
{
    [ViewVariables]
    public SoundSpecifier DeathAudio = new SoundPathSpecifier("/Audio/ADT/Items/TheManWhoSoldTheWorld/death.ogg");
}
