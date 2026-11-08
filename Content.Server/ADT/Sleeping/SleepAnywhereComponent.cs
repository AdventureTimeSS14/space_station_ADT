namespace Content.Server.ADT.Sleeping;

[RegisterComponent]
public sealed partial class SleepAnywhereComponent : Component
{
    [ViewVariables]
    public EntityUid? Action;
}
