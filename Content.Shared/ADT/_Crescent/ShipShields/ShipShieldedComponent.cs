namespace Content.Shared.ADT._Crescent.ShipShields;

[RegisterComponent]
public sealed partial class ShipShieldedComponent : Component
{
    public EntityUid Shield;
    public EntityUid? Source;
}
