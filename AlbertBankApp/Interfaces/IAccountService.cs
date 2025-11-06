using AlbertBankApp.Domain;

namespace AlbertBankApp.Interfaces;

/// <summary>
/// Provides methods for managing bank accounts, including creation, transactions, and retrieval
/// </summary>
public interface IAccountService
{
    Task<IReadOnlyList<BankAccount>> GetAccountsAsync();
    Task DepositAsync(Guid accountId, decimal amount, string? note = null);
    Task WithdrawAsync(Guid accountId, decimal amount, string? note = null);
    Task<IReadOnlyList<Domain.Transaction>> GetTransactionsAsync(Guid accountId);
    Task DeleteAccountAsync(Guid accountId);
    Task TransferAsync(Guid fromAccountId, Guid toAccountId, decimal amount);
    Task DeleteTransactionAsync(Guid txId);
    List<IBankAccount> GetAccounts();
    Task SaveAccountsAsync();
    Task<bool> ValidatePinAsync(string pin);
    Task<string> ExportJsonAsync();
    Task<List<string>?> ImportJsonAsync(string json, bool replace);
    Task ApplyAnnualInterestAsync();
}