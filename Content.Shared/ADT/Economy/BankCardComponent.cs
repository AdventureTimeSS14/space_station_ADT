using Content.Shared.Cargo.Prototypes;
using Robust.Shared.Prototypes;
using Robust.Shared.GameStates;

namespace Content.Shared.ADT.Economy;

[RegisterComponent, NetworkedComponent]
public sealed partial class BankCardComponent : Component
{
    [DataField]
    public int? AccountId;

    [DataField]
    public int StartingBalance;

    [DataField]
    public bool CommandBudgetCard;

    [DataField]
    public ProtoId<CargoAccountPrototype>? CommandBudgetType;
}
