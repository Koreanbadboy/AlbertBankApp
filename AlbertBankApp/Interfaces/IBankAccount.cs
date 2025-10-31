namespace AlbertBankApp.Interfaces;

/// <summary>
/// Represents the contract for a bank account, including properties and methods for managing account details and transactions.
/// </summary>
public interface IBankAccount
{
    Guid Id { get; }
    string Name { get; }
    AccountType AccountType { get; }
    CurrencyType Currency { get; }
    decimal Balance { get; }
    DateTime LastUpdated { get; }
    IReadOnlyList<Transaction> Transactions { get; } 
    
    void Deposit(decimal amount, Guid fromAccountId, string fromAccountName);
    void Withdraw(decimal amount, Guid toAccountId, string toAccountName);
}