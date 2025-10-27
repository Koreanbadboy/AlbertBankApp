using System.Transactions;
namespace AlbertBankApp.Domain;

/// <summary>
/// 
/// </summary>
public class BankAccount : IBankAccount
{
    //Constants
    public Guid Id { get; set; }
    public string Name { get; set; }
    public AccountType AccountType { get; set; }
    public CurrencyType Currency { get; set; }
    public decimal Balance { get; set; }
    public DateTime LastUpdated { get; private set; }
    IReadOnlyList<Transaction> IBankAccount.Transactions { get; set; }
    public object Transaction { get; set; }
    //constructor
    public void Deposit(decimal amount, Guid fromAccountId, string fromAccountName, string description)
    {
        _bankAccountImplementation.Deposit(amount, fromAccountId, fromAccountName, description);
    }
    /// <summary>
    /// Withdraw specific amount from the bankaccount balance
    /// </summary>
    /// <param name="amount">The specified amount</param>
    /// <param name="toAccountId"></param>
    /// <param name="toAccountName"></param>
    /// <param name="description"></param>
    public void Withdraw(decimal amount, Guid toAccountId, string toAccountName, string description)
    {
        _bankAccountImplementation.Withdraw(amount, toAccountId, toAccountName, description);
    }

    public List<Transaction> Transactions { get; set; } = new();
    private readonly List<Transaction> _transactions = new(); //arber
    private IBankAccount _bankAccountImplementation;
    
    /// <summary>
    /// Deposit the specific amount from the bankaccount
    /// </summary>
    /// <param name="amount"></param>
    /// <param name="note"></param>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    public void Deposit(decimal amount, string? note = null)
    {
        if(amount <= 0) 
            throw new ArgumentOutOfRangeException(nameof(amount), "Beloppet måste vara positivt");
        
        Balance += amount;
        Transactions.Add(new Transaction
        {
            Id = Guid.NewGuid(),
            TimeStamp = DateTime.UtcNow,
            Amount = amount,
            ToAccountId = this.Id, // Tillagd - visar att pengarna går till detta konto
            TransactionType = TransactionType.Deposit, // Tillagd - explicit typ
            Note = note ?? "Insättning",
        });
    }

    // ÄNDRAT Uppdaterad idag - Lägger till korrekt TransactionType och FromAccountId
    public void Withdraw(decimal amount, string? note = null)
    {
        if(amount < 0) throw new ArgumentOutOfRangeException(nameof(amount),"Beloppet måste vara positivt");
        if(amount > Balance) throw new InvalidOperationException("Otillräckliga medel för uttag.");
        Balance -= amount;
        Transactions.Add(new Transaction
        {
            Id = Guid.NewGuid(),
            TimeStamp = DateTime.UtcNow,
            Amount = amount, 
            FromAccountId = this.Id, 
            TransactionType = TransactionType.Withdrawal, 
            Note = note ?? "Uttag",
        });
    }
}