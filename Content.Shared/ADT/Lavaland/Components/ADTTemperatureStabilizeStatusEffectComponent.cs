namespace Content.Shared.ADT.Lavaland.Components;

[RegisterComponent]
public sealed partial class ADTTemperatureStabilizeStatusEffectComponent : Component
{
    [DataField]
    public TimeSpan Interval = TimeSpan.FromSeconds(1);

    [DataField]
    public float Step = 3f;

    [ViewVariables]
    public EntityUid? Target;

    [ViewVariables]
    public TimeSpan NextTick;
}
