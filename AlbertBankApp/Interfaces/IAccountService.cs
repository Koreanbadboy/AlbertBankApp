using AlbertBankApp.Domain;

namespace AlbertBankApp.Interfaces;

/// <summary>
/// Provides methods for managing bank accounts, including creation, transactions, and retrieval
/// </summary>
public interface IAccountService
{
    Task<IReadOnlyList<BankAccount>> GetAccountsAsync();
    Task<BankAccount> CreateAccountAsync(string name, AccountType accountType, CurrencyType currency,
        IReadOnlyList<Transaction> initialBalance, decimal? interestRate = null);
    Task DepositAsync(Guid accountId, decimal amount, string? note = null);
    Task WithdrawAsync(Guid accountId, decimal amount, string? note = null);
    Task<IReadOnlyList<Domain.Transaction>> GetTransactionsAsync(Guid accountId);
    Task DeleteAccountAsync(Guid accountId);
    Task TransferAsync(Guid fromAccountId, Guid toAccountId, decimal amount);
    Task DeleteTransactionAsync(Guid txId);
    List<IBankAccount> GetAccounts();
    Task SaveAccountsAsync();
    Task<bool> ValidatePinAsync(string pin);
    Task AdjustBalanceByPercentAsync(Guid accountId, bool increase);
}