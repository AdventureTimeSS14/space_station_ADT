using Robust.Shared.Audio;
using Robust.Shared.GameStates;

namespace Content.Shared.ADT.Mining.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class ADTMiningToolComponent : Component
{
    [DataField]
    public float ToolSpeed = 1f;

    [DataField]
    public SoundSpecifier? UseSound;
}
