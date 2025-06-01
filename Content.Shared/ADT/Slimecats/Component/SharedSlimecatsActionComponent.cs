namespace Content.Shared.ADT.Slimecats;

[RegisterComponent, AutoGenerateComponentState]
public sealed partial class SharedSlimecatsSleepActionComponent : Component
{
    public string SleepActionForSlimecats = "ADTActionSlimeCatsSleep";
    public EntityUid? ActionEntity;

    [ViewVariables, AutoNetworkedField]
    public bool IsActiveSleep = false;
}