namespace Content.Server.ADT.Salvage.Components;

[RegisterComponent]
public sealed partial class TendrilMobComponent : Component
{
    [ViewVariables]
    public EntityUid? Tendril;

    [ViewVariables]
    public float? BaseAggroRadius;
}
