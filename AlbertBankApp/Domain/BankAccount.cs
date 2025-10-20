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
            ToAccountId = this.Id,
            TransactionType = TransactionType.Deposit,
            Note = note ?? "Insättning",
        });
    }

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