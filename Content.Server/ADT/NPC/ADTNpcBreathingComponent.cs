namespace Content.Server.ADT.NPC;

[RegisterComponent, Access(typeof(ADTNpcBreathingSystem))]
public sealed partial class ADTNpcBreathingComponent : Component
{
    [DataField]
    public TimeSpan CheckInterval = TimeSpan.FromSeconds(1);

    [DataField]
    public TimeSpan MaskRaiseRecheck = TimeSpan.FromSeconds(0.25);

    [DataField]
    public TimeSpan SafeDelay = TimeSpan.FromSeconds(5);

    [DataField]
    public TimeSpan ClassifyInterval = TimeSpan.FromSeconds(30);

    [DataField]
    public float MinBreathablePressure = 16f;

    [DataField]
    public float ToxicPressure = 1f;

    [ViewVariables]
    public TimeSpan NextCheck;

    [ViewVariables]
    public TimeSpan? SafeSince;

    [ViewVariables]
    public TimeSpan ClassifiedUntil;

    [ViewVariables]
    public bool[]? BreathableGases;

    [ViewVariables]
    public bool[]? ToxicGases;
}
