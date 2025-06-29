using Content.Shared.Cargo.Prototypes;
using Content.Shared.Mind;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Shared.ADT.Economy;

public sealed class BankAccount
{
    public readonly int AccountId;
    public readonly int AccountPin;
    public int Balance;
    public bool CommandBudgetAccount;
    public Entity<MindComponent>? Mind;
    public string Name = string.Empty;
    public ProtoId<CargoAccountPrototype>? AccountPrototype;
    public EntityUid? CartridgeUid;

    public BankAccount(int accountId, int balance, IRobustRandom random)
    {
        AccountId = accountId;
        Balance = balance;
        AccountPin = random.Next(1000, 10000);
    }
}

