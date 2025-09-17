using Content.Server.GameTicking;
using Content.Shared.ADT.Economy;
using Content.Shared.GameTicking;
using Robust.Shared.Timing;

namespace Content.Server.ADT.Economy;

/// <summary>
/// Система управления историей банковских транзакций
/// </summary>
public sealed class BankTransactionHistorySystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly BankCartridgeSystem _bankCartridgeSystem = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<RoundRestartCleanupEvent>(OnRoundRestart);
    }

    /// <summary>
    /// Записывает новую транзакцию в историю аккаунта
    /// </summary>
    public void RecordTransaction(int accountId, BankTransactionType type, int amount, int balanceAfter, string description, string? details = null)
    {
        var transaction = new BankTransaction
        {
            Timestamp = _timing.CurTime,
            Type = type,
            Amount = amount,
            BalanceAfter = balanceAfter,
            Description = description,
            Details = details
        };

        // Находим все картриджи, связанные с этим аккаунтом
        var query = EntityQueryEnumerator<BankCartridgeComponent>();
        while (query.MoveNext(out var uid, out var cartridge))
        {
            if (cartridge.AccountId != accountId)
                continue;

            // Получаем или создаем компонент истории транзакций
            var historyComp = EnsureComp<BankTransactionHistoryComponent>(uid);

            // Добавляем новую транзакцию
            historyComp.Transactions.Add(transaction);

            // Удаляем старые транзакции, если превышен лимит
            if (historyComp.Transactions.Count > historyComp.MaxTransactions)
            {
                var toRemove = historyComp.Transactions.Count - historyComp.MaxTransactions;
                historyComp.Transactions.RemoveRange(0, toRemove);
            }

            // Помечаем компонент как измененный для сетевой синхронизации
            Dirty(uid, historyComp);

            // Обновляем UI картриджа
            _bankCartridgeSystem.UpdateUiState(uid);
        }
    }

    /// <summary>
    /// Получает историю транзакций для аккаунта
    /// </summary>
    public List<BankTransaction> GetTransactionHistory(int accountId)
    {
        var query = EntityQueryEnumerator<BankCartridgeComponent, BankTransactionHistoryComponent>();
        while (query.MoveNext(out var uid, out var cartridge, out var history))
        {
            if (cartridge.AccountId == accountId)
            {
                return new List<BankTransaction>(history.Transactions);
            }
        }

        return new List<BankTransaction>();
    }

    /// <summary>
    /// Очищает всю историю транзакций при перезапуске раунда
    /// </summary>
    private void OnRoundRestart(RoundRestartCleanupEvent ev)
    {
        var query = EntityQueryEnumerator<BankTransactionHistoryComponent>();
        while (query.MoveNext(out var uid, out var history))
        {
            history.Transactions.Clear();
            Dirty(uid, history);
        }
    }
}
