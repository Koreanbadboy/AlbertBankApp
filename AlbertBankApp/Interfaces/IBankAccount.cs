namespace AlbertBankApp.Interfaces;

public interface IBankAccount
{
    Guid Id { get; }                    // get-only (använd ctor för att sätta)
    string Name { get; }
    AccountType AccountType { get; }
    CurrencyType Currency { get; }
    decimal Balance { get; }
    DateTime LastUpdated { get; }
    IReadOnlyList<Transaction> Transactions { get; }  // get-only

    // Rena, korta API:t
    void Deposit(decimal amount, string? note = null);
    void Withdraw(decimal amount, string? note = null);

    // Legacy-signaturer för kompatibilitet (mappar vi explicit i klassen)
    void Deposit(decimal amount, Guid fromAccountId, string fromAccountName, string description);
    void Withdraw(decimal amount, Guid toAccountId, string toAccountName, string description);
}