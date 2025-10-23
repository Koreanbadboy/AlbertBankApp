using System.Transactions;

namespace AlbertBankApp.Domain;

public class BankAccount : IBankAccount
{
    public Guid Id { get; set; }
    public string Name { get; set; }
    public AccountType AccountType { get; set; }
    public CurrencyType Currency { get; set; }
    public decimal Balance { get; set; }
    public DateTime LastUpdated { get; private set; }
    IReadOnlyList<Transaction> IBankAccount.Transactions { get; set; }
    public object Transaction { get; set; }
    public void Deposit(decimal amount, Guid fromAccountId, string fromAccountName, string description)
    {
        _bankAccountImplementation.Deposit(amount, fromAccountId, fromAccountName, description);
    }

    public void Withdraw(decimal amount, Guid toAccountId, string toAccountName, string description)
    {
        _bankAccountImplementation.Withdraw(amount, toAccountId, toAccountName, description);
    }

    public List<Transaction> Transactions { get; set; } = new();
    private readonly List<Transaction> _transactions = new(); //arber
    private IBankAccount _bankAccountImplementation;


    // ÄNDRAT Uppdaterad idag - Lägger till korrekt TransactionType och ToAccountId
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
            Amount = amount, // ÄNDRAT från -amount (negativt belopp sparas inte längre)
            FromAccountId = this.Id, // Tillagd - visar att pengarna går från detta konto
            TransactionType = TransactionType.Withdrawal, // Tillagd - explicit typ
            Note = note ?? "Uttag",
        });
    }
}