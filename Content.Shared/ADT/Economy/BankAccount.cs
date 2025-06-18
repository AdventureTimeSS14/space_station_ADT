using Content.Shared.Cargo.Prototypes;
using Content.Shared.Mind;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Shared.ADT.Economy;

public sealed class BankAccount
{
    [Dependency] private readonly IRobustRandom _random = default!;

    public readonly int AccountId;
    public readonly int AccountPin;
    public int Balance;
    public bool CommandBudgetAccount;
    public Entity<MindComponent>? Mind;
    public string Name = string.Empty;
    public ProtoId<CargoAccountPrototype>? AccountPrototype;
    public EntityUid? CartridgeUid;

    public BankAccount(int accountId, int balance)
    {
        AccountId = accountId;
        Balance = balance;
        AccountPin = _random.Next(1000, 10000);
    }
}

