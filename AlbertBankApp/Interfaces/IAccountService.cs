using AlbertBankApp.Domain;

namespace AlbertBankApp.Interfaces;

public interface IAccountService
{
    Task<IReadOnlyList<BankAccount>> GetAccountsAsync();
    Task<BankAccount> CreateAccountAsync(string name, AccountType accountType, CurrencyType currency, decimal initialBalance = 0m);
    Task DepositAsync(Guid accountId, decimal amount, string? note = null);
    Task WithdrawAsync(Guid accountId, decimal amount, string? note = null);
    Task<IReadOnlyList<Domain.Transaction>> GetTransactionsAsync(Guid accountId);
    Task DeleteAccountAsync(Guid accountId);
    Task TransferAsync(Guid fromAccountId, Guid toAccountId, decimal amount);
    Task DeleteTransactionAsync(Guid txId);
    List<IBankAccount> GetAccounts();
    Task SaveAccountsAsync();
}