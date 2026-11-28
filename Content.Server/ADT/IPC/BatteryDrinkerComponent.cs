namespace Content.Server.ADT.IPC;

[RegisterComponent]
public sealed partial class BatteryDrinkerComponent : Component
{
    [DataField]
    public bool DrinkAll;

    [DataField]
    public float DrinkSpeed = 1.5f;

    [DataField]
    public float DrinkAllMultiplier = 2.5f;
}
