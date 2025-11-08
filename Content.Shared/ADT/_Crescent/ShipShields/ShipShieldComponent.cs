namespace Content.Shared.ADT._Crescent.ShipShields;

[RegisterComponent]
public sealed partial class ShipShieldComponent : Component
{
    public EntityUid? Source;
    public EntityUid Shielded;
}
