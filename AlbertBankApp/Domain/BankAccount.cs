using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;

namespace AlbertBankApp.Domain;

public class BankAccount : IBankAccount
{
    private List<Transaction> _transactions = new List<Transaction>();

    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public AccountType AccountType { get; set; }
    public CurrencyType Currency { get; set; }
    public decimal Balance { get; set; }
    public decimal? InterestRate { get; set; }
    public decimal InitialBalance { get; set; }
    public DateTime LastUpdated { get; set; }
    
    public IReadOnlyList<Transaction> Transactions
    {
        get => _transactions;
        set => _transactions = value != null ? new List<Transaction>(value) : new List<Transaction>();
    }

    [JsonConstructor]
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
        _transactions = transactions != null ? new List<Transaction>(transactions) : new List<Transaction>();
    }
    
    public void Deposit(decimal amount, Guid transactionId, string note)
    {
        if (amount <= 0) throw new ArgumentOutOfRangeException(nameof(amount));
        if (transactionId == Guid.Empty) transactionId = Guid.NewGuid();

        var now = DateTime.UtcNow;
        var before = Balance;
        Balance += amount;

        var tx = new Transaction
        {
            Id = transactionId,
            TimeStamp = now,
            Amount = amount,
            ToAccountId = Id,
            TransactionType = TransactionType.Deposit,
            Note = note ?? "Deposit",
            BalanceBefore = before,
            BalanceAfter = Balance,
            ToAccountName = Name,
            LastUpdated = now
        };

        _transactions.Add(tx);
        LastUpdated = now;
    }

    public void Withdraw(decimal amount, Guid transactionId, string note)
    {
        if (amount <= 0) throw new ArgumentOutOfRangeException(nameof(amount));
        if (amount > Balance) throw new InvalidOperationException("Insufficient funds");
        if (transactionId == Guid.Empty) transactionId = Guid.NewGuid();

        var now = DateTime.UtcNow;
        var before = Balance;
        Balance -= amount;

        var tx = new Transaction
        {
            Id = transactionId,
            TimeStamp = now,
            Amount = amount,
            FromAccountId = Id,
            TransactionType = TransactionType.Withdrawal,
            Note = note ?? "Withdrawal",
            BalanceBefore = before,
            BalanceAfter = Balance,
            FromAccountName = Name,
            LastUpdated = now
        };

        _transactions.Add(tx);
        LastUpdated = now;
    }
    public void Deposit(decimal amount, string? note = null) => Deposit(amount, Guid.NewGuid(), note ?? "Deposit");
    public void Withdraw(decimal amount, string? note = null) => Withdraw(amount, Guid.NewGuid(), note ?? "Withdrawal");
    
    internal void AddInternalTransaction(Transaction tx) => _transactions.Add(tx);

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

        _transactions.Add(tx);
        target.AddInternalTransaction(tx);

        LastUpdated = now;
        target.LastUpdated = now;
    }

    public void RemoveTransaction(Guid transactionId)
    {
        var tx = _transactions.FirstOrDefault(t => t.Id == transactionId);
        if (tx == null) return;
        _transactions.Remove(tx);
        
        decimal b = InitialBalance;
        foreach (var t in _transactions)
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
        }
    }
}
