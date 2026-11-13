namespace Content.Shared.ADT.Crushers.Components;

[RegisterComponent]
public sealed partial class AdaptiveLearningActiveEffectComponent : Component
{
    [ViewVariables]
    public float Bonus;

    [ViewVariables]
    public TimeSpan ExpireTime;
}
