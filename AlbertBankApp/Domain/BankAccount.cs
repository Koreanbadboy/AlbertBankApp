using System.Transactions;

namespace AlbertBankApp.Domain;

public class BankAccount
{
    public Guid Id { get; set; }
    public string Name { get; set; }
    public AccountType AccountType { get; set; }
    public CurrencyType Currency { get; set; }
    public decimal Balance { get; set; }
    public DateTime LastUpdated { get; private set; }
    public List<Transaction> Transactions { get; set; } = new();

    // Uppdaterad idag - Lägger till korrekt TransactionType och ToAccountId
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

    // Uppdaterad idag - Lägger till korrekt TransactionType och FromAccountId
    public void Withdraw(decimal amount, string? note = null)
    {
        if(amount < 0) throw new ArgumentOutOfRangeException(nameof(amount),"Beloppet måste vara positivt");
        if(amount > Balance) throw new InvalidOperationException("Otillräckliga medel för uttag.");
        Balance -= amount;
        Transactions.Add(new Transaction
        {
            Id = Guid.NewGuid(),
            TimeStamp = DateTime.UtcNow,
            Amount = amount, // Ändrat från -amount (negativt belopp sparas inte längre)
            FromAccountId = this.Id, // Tillagd - visar att pengarna går från detta konto
            TransactionType = TransactionType.Withdrawal, // Tillagd - explicit typ
            Note = note ?? "Uttag",
        });
    }
}