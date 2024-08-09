using Robust.Shared.GameStates;


namespace Content.Shared.TransformAnimation;

[RegisterComponent, NetworkedComponent]
public sealed partial class TransformAnimationComponent : Component
{
    [DataField("transformingTo", required: true)]
    public string? TransformingTo;
    [DataField("duration", required: true)]
    public float Duration = 0f;

    public TimeSpan TransformTime = TimeSpan.Zero;
}
