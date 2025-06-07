namespace Content.Server.ADT.Administration;

[RegisterComponent, AutoGenerateComponentState]
public sealed partial class AdminGodComponent : Component
{
    public string ActionForAdminGod1 = "ActionRevenantSmoke";
    public string ActionForAdminGod2 = "ActionRevenantLock";
    public string ActionForAdminGod3 = "ActionLingRegenerate";
    public EntityUid? ActionEntity;
}