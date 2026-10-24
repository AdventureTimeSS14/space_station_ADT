namespace Content.Server.ADT.Heretic.Components;

[RegisterComponent]
public sealed partial class AshAscendedComponent : Component
{
    [DataField]
    public float FireInterval = 2f;

    [DataField]
    public float Accumulator;

    [DataField]
    public float Radius = 2.5f;
}