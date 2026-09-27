using Robust.Shared.GameObjects;

namespace Content.Shared.ADT.Economy;

[RegisterComponent]
public sealed partial class StartingBalanceComponent : Component
{
    [DataField]
    public int Balance;
}