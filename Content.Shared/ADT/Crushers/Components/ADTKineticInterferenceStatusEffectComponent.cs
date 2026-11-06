namespace Content.Shared.ADT.Crushers.Components;

[RegisterComponent]
public sealed partial class ADTKineticInterferenceStatusEffectComponent : Component
{
    [DataField]
    public float CooldownMultiplier = 1.5f;
}
