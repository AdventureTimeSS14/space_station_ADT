using Robust.Shared.Audio;
using Robust.Shared.GameStates;

namespace Content.Shared.ADT.Bubblegum.Abilities;

[RegisterComponent, NetworkedComponent]
public sealed partial class BubblegumBloodWarpComponent : Component
{
    [DataField]
    public float OuterRange = 5f;

    [DataField]
    public float InnerRange = 4f;

    [DataField]
    public float SelfRange = 1f;

    [DataField]
    public float AdjacentRange = 1.5f;

    [DataField]
    public float TargetSearchRange = 20f;

    [DataField]
    public float SinkTime = 0.5f;

    [DataField]
    public SoundSpecifier EnterSound = new SoundPathSpecifier("/Audio/ADT/Bubblegum/enter_blood.ogg");

    [DataField]
    public SoundSpecifier ExitSound = new SoundPathSpecifier("/Audio/ADT/Bubblegum/exit_blood.ogg");
}
