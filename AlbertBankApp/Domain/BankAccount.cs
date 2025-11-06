using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;

namespace AlbertBankApp.Domain;

public class BankAccount
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public AccountType AccountType { get; set; }
    public CurrencyType Currency { get; set; }
    public decimal Balance { get; set; }
    public decimal? InterestRate { get; set; }
    public decimal InitialBalance { get; set; }
    public DateTime LastUpdated { get; set; }
    public List<Transaction> Transactions { get; set; } = new List<Transaction>();

    // Parameterless constructor for JSON deserialization
    public BankAccount()
    {
    }

    // Constructor for runtime creation
    public BankAccount(
        Guid id,
        string name,
        AccountType accountType,
        CurrencyType currency,
        decimal balance,
        decimal initialBalance,
        DateTime lastUpdated,
        List<Transaction>? transactions,
        decimal? interestRate)
    {
        Id = id;
        Name = name ?? string.Empty;
        AccountType = accountType;
        Currency = currency;
        Balance = balance;
        InitialBalance = initialBalance;
        LastUpdated = lastUpdated;
        InterestRate = interestRate;
        Transactions = transactions != null ? new List<Transaction>(transactions) : new List<Transaction>();
    }
    

    public void Deposit(decimal amount, string? note = null)
    {
        if (amount <= 0) throw new ArgumentOutOfRangeException(nameof(amount));
        var now = DateTime.UtcNow;
        var before = Balance;
        Balance += amount;
        Transactions.Add(new Transaction
        {
            Id = Guid.NewGuid(),
            TimeStamp = now,
            Amount = amount,
            ToAccountId = Id,
            TransactionType = TransactionType.Deposit,
            Note = note ?? "Deposit",
            BalanceBefore = before,
            BalanceAfter = Balance,
            ToAccountName = Name,
            LastUpdated = now
        });
        LastUpdated = now;
    }

    public void Withdraw(decimal amount, string? note = null)
    {
        if (amount <= 0) throw new ArgumentOutOfRangeException(nameof(amount));
        if (amount > Balance) throw new InvalidOperationException("Insufficient funds");
        var now = DateTime.UtcNow;
        var before = Balance;
        Balance -= amount;
        Transactions.Add(new Transaction
        {
            Id = Guid.NewGuid(),
            TimeStamp = now,
            Amount = amount,
            FromAccountId = Id,
            TransactionType = TransactionType.Withdrawal,
            Note = note ?? "Withdrawal",
            BalanceBefore = before,
            BalanceAfter = Balance,
            FromAccountName = Name,
            LastUpdated = now
        });
        LastUpdated = now;
    }

    public void TransferTo(BankAccount target, decimal amount)
    {
        if (target == null) throw new ArgumentNullException(nameof(target));
        if (amount <= 0) throw new ArgumentOutOfRangeException(nameof(amount));
        if (amount > Balance) throw new InvalidOperationException("Insufficient funds");

        var now = DateTime.UtcNow;
        var beforeSource = Balance;

        Balance -= amount;
        target.Balance += amount;

        var tx = new Transaction
        {
            Id = Guid.NewGuid(),
            TimeStamp = now,
            Amount = amount,
            FromAccountId = Id,
            ToAccountId = target.Id,
            TransactionType = TransactionType.Transfer,
            Note = $"Transfer from {Name} to {target.Name}",
            BalanceBefore = beforeSource,
            BalanceAfter = Balance,
            FromAccountName = Name,
            ToAccountName = target.Name,
            LastUpdated = now
        };

        Transactions.Add(tx);
        target.Transactions.Add(tx);

        LastUpdated = now;
        target.LastUpdated = now;
    }

    public void RemoveTransaction(Guid transactionId)
    {
        var tx = Transactions.FirstOrDefault(t => t.Id == transactionId);
        if (tx == null) return;
        Transactions.Remove(tx);

        // Recalculate balance from initial balance + transactions
        decimal b = InitialBalance;
        foreach (var t in Transactions)
        {
            if (t.ToAccountId == Id) b += t.Amount;
            if (t.FromAccountId == Id) b -= t.Amount;
        }
        Balance = b;
        LastUpdated = DateTime.UtcNow;
    }
    
    public void ApplyInterest()
    {
        if (AccountType == AccountType.Sparkonto && InterestRate.HasValue)
        {
            Balance *= (1 + InterestRate.Value);
            // Optionally add a transaction record here if needed
        }
    }
}
