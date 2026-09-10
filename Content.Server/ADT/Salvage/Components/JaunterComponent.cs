namespace Content.Server.ADT.Salvage.Components;

[RegisterComponent]
public sealed partial class JaunterComponent : Component
{
    [DataField]
    public bool DeleteOnUse = true;

    [DataField]
    public TimeSpan ChasmImmunity = TimeSpan.FromSeconds(3);

    public bool BeaconMode = false;
}

[RegisterComponent]
public sealed partial class JaunterPortalComponent : Component
{
    public float AfterEnterLifetime = 3f;
}

[RegisterComponent]
public sealed partial class JaunterKillPortalComponent : Component { }
