using Content.Server.ADT.GameTicking.Rules;

namespace Content.Server.ADT.ShipsVsShips;

[RegisterComponent]
public sealed partial class ShipsVsShipsShuttleComponent : Component
{
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public Side Side = Side.Attackers;

    [DataField]
    public bool FtlToAttack;
}
