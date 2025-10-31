using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using AlbertBankApp.Interfaces;

namespace AlbertBankApp.Domain;

/// <summary>
/// Represents a bank account with balance management, transaction history, and support for deposits, withdrawals, and transfers.
/// </summary>
public class BankAccount : IBankAccount
{
    private readonly List<Transaction> _transactions;
    public Guid Id { get; private set; } = Guid.NewGuid();
    public string Name { get; private set; } = string.Empty;
    public AccountType AccountType { get; private set; }
    public CurrencyType Currency { get; private set; }
    public decimal Balance { get; set; }
    public decimal? InterestRate { get; internal set; } 
    public decimal InitialBalance { get; internal set; }
    public DateTime LastUpdated { get; private set; }
    public IReadOnlyList<Transaction> Transactions => _transactions.AsReadOnly();

    public void UpdateBalanceWithInterest()
    {
        if (!InterestRate.HasValue) return;
        InitialBalance = InitialBalance * (1 + InterestRate.Value);
    }
    
    /// <summary>
    ///  Initializes a new instance of the "BankAccount" class with specified parameters
    /// </summary>
    /// <param name="id"></param>
    /// <param name="name"></param>
    /// <param name="accountType"></param>
    /// <param name="currency"></param>
    /// <param name="initialBalance"></param>
    /// <param name="lastUpdated"></param>
    /// <param name="transactions"></param>
    [JsonConstructor]
    public BankAccount(Guid id, string name, AccountType accountType, CurrencyType currency, decimal initialBalance, DateTime lastUpdated, IReadOnlyList<Transaction> transactions)
    {
        Id = id;
        Name = name;
        AccountType = accountType;
        Currency = currency;
        InitialBalance = initialBalance;
        LastUpdated = lastUpdated;
        _transactions = transactions != null ? new List<Transaction>(transactions) : new List<Transaction>();
    }

    /// <summary>
    /// Specific account for deposit
    /// </summary>
    /// <param name="amount">Specific amount</param>
    /// <param name="note">Optinal note or description for the deposit</param>
    /// <exception cref="ArgumentOutOfRangeException">Error message if deposit balace is negativ</exception>
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

    /// <summary>
    /// Withdraws a specified amount from this account, with an optional note.
    /// </summary>
    /// <param name="amount">The amount to withdraw.</param>
    /// <param name="note">Optional note or description for the withdrawal.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown if the amount is not positive.</exception>
    /// <exception cref="InvalidOperationException">Thrown if the account has insufficient funds.</exception>
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

    /// <summary>
    /// Transfers a specified amount from this account to a target account, with an optional note.
    /// </summary>
    /// <param name="target">The target bank account to transfer funds to.</param>
    /// <param name="amount">The amount to transfer.</param>
    /// <param name="note">Optional note or description for the transfer.</param>
    /// <exception cref="ArgumentNullException">Thrown if the target account is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown if transferring to the same account or if funds are insufficient.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown if the amount is not positive.</exception>
    public void TransferTo(BankAccount target, decimal amount, string? note = null)
    {
        if (target is null) throw new ArgumentNullException(nameof(target));
        if (target.Id == Id) throw new InvalidOperationException("Kan inte överföra till samma konto.");
        if (amount <= 0) throw new ArgumentOutOfRangeException(nameof(amount), "Beloppet måste vara positivt");
        if (amount > Balance) throw new InvalidOperationException("Otillräckliga medel för överföring.");

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

    /// <summary>
    /// Deposits a specified amount into this account from another account, with an optional description.
    /// </summary>
    /// <param name="amount">The amount to deposit.</param>
    /// <param name="fromAccountId">The ID of the account from which the funds are coming.</param>
    /// <param name="fromAccountName">The name of the account from which the funds are coming.</param>
    /// <param name="description">Optional description or note for the deposit.</param>
    void IBankAccount.Deposit(decimal amount, Guid fromAccountId, string fromAccountName, string description)
        => Deposit(amount, string.IsNullOrWhiteSpace(description)
            ? $"Insättning från {fromAccountName}"
            : description);
    
    /// <summary>
    /// Withdraws a specified amount from this account to another account, with an optional description.
    /// </summary>
    /// <param name="amount">The amount to withdraw.</param>
    /// <param name="toAccountId">The ID of the account to which the funds are sent.</param>
    /// <param name="toAccountName">The name of the account to which the funds are sent.</param>
    /// <param name="description">Optional description or note for the withdrawal.</param>
    void IBankAccount.Withdraw(decimal amount, Guid toAccountId, string toAccountName, string description)
        => Withdraw(amount, string.IsNullOrWhiteSpace(description)
            ? $"Uttag till {toAccountName}"
            : description);

    /// <summary>
    /// Removes a transaction from the account by its unique identifier and updates the balance.
    /// </summary>
    /// <param name="transactionId">The unique identifier of the transaction to remove.</param>
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
    
    /// <summary>
    /// Initializes a new instance of the "BankAccount" class with default values
    /// </summary>
    public BankAccount(IReadOnlyList<Transaction> transactions)
    {
        _transactions = transactions != null ? new List<Transaction>(transactions) : new List<Transaction>();
        Id = Guid.NewGuid();
        Name = string.Empty;
        AccountType = default;
        Currency = default;
        Balance = 0m;
        LastUpdated = DateTime.UtcNow;
    }
    
    /// <summary>
    ///  Initializes a new instance of the "BankAccount" class with specified parameters
    /// </summary>
    /// <param name="id"></param>
    /// <param name="name"></param>
    /// <param name="accountType"></param>
    /// <param name="currency"></param>
    /// <param name="transactions"></param>
    /// <param name="initialBalance"></param>
    /// <param name="interestRate"></param>
    /// <exception cref="ArgumentNullException"></exception>
    public BankAccount(Guid id, string name, AccountType accountType, CurrencyType currency, IReadOnlyList<Transaction> transactions, decimal initialBalance = 0m, decimal? interestRate = null)
    {
        _transactions = transactions != null ? new List<Transaction>(transactions) : new List<Transaction>();
        Id = id == default ? Guid.NewGuid() : id;
        Name = name ?? throw new ArgumentNullException(nameof(name));
        AccountType = accountType;
        Currency = currency;
        InitialBalance = initialBalance;
        InterestRate = interestRate;
        Balance = 0m;
        LastUpdated = DateTime.UtcNow;

        if (initialBalance > 0)
            Deposit(initialBalance, "Initial balance");
    }
    /// <summary>
    /// InterestRate method for Sparkonto account
    /// </summary>
public void ApplyInterest()
    {
        if (AccountType == AccountType.Sparkonto && InterestRate.HasValue && InterestRate != 0)
        {
            var interest = Balance * InterestRate.Value;
            var newBalance = Balance + interest;
    
            if (newBalance < InitialBalance)
            {
                Balance = InitialBalance;
                // Optionally, add a transaction for the adjustment if you want it in history
            }
            else if (interest > 0)
            {
                Deposit(interest, "Ränteinsättning");
            }
            else if (-interest <= Balance)
            {
                Withdraw(-interest, "Negativ ränta");
            }
        }
    }
}