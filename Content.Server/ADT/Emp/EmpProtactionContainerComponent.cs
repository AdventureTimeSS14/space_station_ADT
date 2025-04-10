namespace Content.Server.ADT.EMPProtaction;

[RegisterComponent]
public sealed partial class EmpContainerProtactionComponent : Component
{
    public EntityUid? BatteryUid;
    [DataField]
    public string ContainerId = "cell_slot";
}
