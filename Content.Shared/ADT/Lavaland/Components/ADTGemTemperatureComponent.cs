namespace Content.Shared.ADT.Lavaland.Components;

[RegisterComponent]
public sealed partial class ADTGemTemperatureComponent : Component
{
    [DataField(required: true)]
    public float Delta;

    [DataField]
    public LocId Message = "adt-gem-temperature-use";
}
