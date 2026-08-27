namespace Content.Shared.Cuffs.Components;

[RegisterComponent]
public sealed partial class LegCuffBreakoutSoundComponent : Component
{
    [ViewVariables]
    public TimeSpan NextAllowedTime;
}
