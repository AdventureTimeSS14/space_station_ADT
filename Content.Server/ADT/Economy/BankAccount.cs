using Content.Shared.Mind;

namespace Content.Server.ADT.Economy;

public sealed class BankAccount
{
    public readonly int AccountId;
    public readonly int AccountPin;
    public int Balance;
    public bool CommandBudgetAccount;
    public Entity<MindComponent>? Mind;
    public string Name = string.Empty;

    public EntityUid? CartridgeUid;

    public BankAccount(int accountId, int balance)
    {
        AccountId = accountId;
        Balance = balance;
        AccountPin = Random.Shared.Next(1000, 10000);
    }
}

