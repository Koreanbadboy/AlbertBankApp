using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using AlbertBankApp.Interfaces;

namespace AlbertBankApp.Domain;

public class BankAccount : IBankAccount
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public AccountType AccountType { get; set; }
    public CurrencyType Currency { get; set; }

    [JsonInclude]
    public decimal Balance { get; private set; }

    [JsonInclude]
    public DateTime LastUpdated { get; private set; }

    [JsonInclude]
    private List<Transaction> _transactions = new();
    public IReadOnlyList<Transaction> Transactions => _transactions.AsReadOnly();

    // EN ren Deposit
    public void Deposit(decimal amount, string? note = null)
    {
        if (amount <= 0) throw new ArgumentOutOfRangeException(nameof(amount), "Beloppet måste vara positivt");
        var now = DateTime.UtcNow;

        var before = Balance;
        Balance += amount;

        _transactions.Add(new Transaction
        {
            Id = Guid.NewGuid(),
            TimeStamp = now,
            Amount = amount,
            ToAccountId = Id,
            TransactionType = TransactionType.Deposit,
            Note = note ?? "Insättning",
            BalanceBefore = before,
            BalanceAfter = Balance,
            ToAccountName = Name,
            LastUpdated = now
        });
        LastUpdated = now;
    }

    // EN ren Withdraw
    public void Withdraw(decimal amount, string? note = null)
    {
        if (amount <= 0) throw new ArgumentOutOfRangeException(nameof(amount), "Beloppet måste vara positivt");
        if (amount > Balance) throw new InvalidOperationException("Otillräckliga medel för uttag.");
        var now = DateTime.UtcNow;

        var before = Balance;
        Balance -= amount;

        _transactions.Add(new Transaction
        {
            Id = Guid.NewGuid(),
            TimeStamp = now,
            Amount = amount,
            FromAccountId = Id,
            TransactionType = TransactionType.Withdrawal,
            Note = note ?? "Uttag",
            BalanceBefore = before,
            BalanceAfter = Balance,
            FromAccountName = Name,
            LastUpdated = now
        });
        LastUpdated = now;
    }

    // Ny: överföring mellan konton – skapar en gemensam Transaction som läggs i båda kontonas internlista
    public void TransferTo(BankAccount target, decimal amount, string? note = null)
    {
        if (target is null) throw new ArgumentNullException(nameof(target));
        if (target.Id == Id) throw new InvalidOperationException("Kan inte överföra till samma konto.");
        if (amount <= 0) throw new ArgumentOutOfRangeException(nameof(amount), "Beloppet måste vara positivt");
        if (amount > Balance) throw new InvalidOperationException("Otillräckliga medel för överföring.");

        var now = DateTime.UtcNow;

        var beforeSource = Balance;

        // Uppdatera saldon
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
            Note = string.IsNullOrWhiteSpace(note) ? $"Överföring från {Name} till {target.Name}" : note,
            BalanceBefore = beforeSource,
            BalanceAfter = Balance,
            FromAccountName = Name,
            ToAccountName = target.Name,
            LastUpdated = now
        };

        _transactions.Add(tx);
        target._transactions.Add(tx);

        LastUpdated = now;
        target.LastUpdated = now;
    }

// ====== Explicit interface-implementation (legacy-signaturer) ======
    void IBankAccount.Deposit(decimal amount, Guid fromAccountId, string fromAccountName, string description)
        => Deposit(amount, string.IsNullOrWhiteSpace(description)
            ? $"Insättning från {fromAccountName}"
            : description);

    void IBankAccount.Withdraw(decimal amount, Guid toAccountId, string toAccountName, string description)
        => Withdraw(amount, string.IsNullOrWhiteSpace(description)
            ? $"Uttag till {toAccountName}"
            : description);

    // Valfritt: behåll dina Transfer/RemoveTransaction om du vill.
    public void RemoveTransaction(Guid transactionId)
    {
        var tx = _transactions.FirstOrDefault(t => t.Id == transactionId);
        if (tx == null) return;

        _transactions.Remove(tx);

        decimal newBalance = 0m;
        foreach (var t in _transactions)
        {
            if (t.ToAccountId == Id) newBalance += t.Amount;
            if (t.FromAccountId == Id) newBalance -= t.Amount;
        }

        Balance = newBalance;
        LastUpdated = DateTime.UtcNow;
    }

    // Parameterless constructor required for JSON serialization/deserialization (LocalStorage)
    // Keeps property setters public so deserializers can populate values.
    public BankAccount()
    {
        // default initialization; real values may be set by deserializer or parameterized ctor
        Id = Guid.NewGuid();
        Name = string.Empty;
        AccountType = default;
        Currency = default;
        Balance = 0m;
        LastUpdated = DateTime.UtcNow;
    }
    public BankAccount(Guid id, string name, AccountType accountType, CurrencyType currency, decimal initialBalance = 0m)
    {
        Id = id == default ? Guid.NewGuid() : id;
        Name = name ?? throw new ArgumentNullException(nameof(name));
        AccountType = accountType;
        Currency = currency;
        Balance = 0m;
        LastUpdated = DateTime.UtcNow;

        if (initialBalance > 0)
            Deposit(initialBalance, "Initial balance");
    }
}