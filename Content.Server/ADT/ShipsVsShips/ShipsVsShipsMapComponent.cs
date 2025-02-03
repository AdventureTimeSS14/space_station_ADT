using Content.Server.ADT.GameTicking.Rules;

namespace Content.Server.ADT.ShipsVsShips;

[RegisterComponent]
public sealed partial class ShipsVsShipsMapComponent : Component
{
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public Side Side = Side.Attackers;
}
