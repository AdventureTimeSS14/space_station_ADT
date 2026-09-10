namespace Content.Server.ADT.Planet.RestrictedZone;

[RegisterComponent]
public sealed partial class ADTRestrictedZoneComponent : Component
{
    [DataField]
    public float TerrainMargin = 8f;

    [DataField]
    public float MarkerMargin;

    [DataField]
    public float SpawnBuffer = 8f;

    [DataField]
    public TimeSpan GuardInterval = TimeSpan.FromSeconds(1);

    [DataField]
    public float PushBack = 2f;

    [ViewVariables]
    public TimeSpan NextGuard;
}
