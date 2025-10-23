namespace AlbertBankApp.Interfaces;

public interface IBankAccount
{
    Guid Id { get; set; }
    string Name { get; }
    public AccountType AccountType { get; }
    CurrencyType Currency { get; }
    decimal Balance { get;}
    DateTime LastUpdated { get; }
    IReadOnlyList<Transaction> Transactions { get; set; }
    object Transaction { get; set; }

    void Deposit(decimal amount, Guid fromAccountId, string fromAccountName, string description);
    void Withdraw(decimal amount, Guid toAccountId, string toAccountName, string description);
}


