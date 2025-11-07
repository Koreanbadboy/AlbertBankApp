using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;

namespace AlbertBankApp.Domain;

/// <summary>
///  A bank account in the banking application
/// </summary>
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
    public DateTime? LastInterestApplied { get; set; }

    /// <summary>
    ///  All transactions related to this bank account
    /// </summary>
    public IReadOnlyList<Transaction> Transactions
    {
        get => _transactions;
        set => _transactions = value != null ? new List<Transaction>(value) : new List<Transaction>();
    }

    /// <summary>
    /// Creates a new bank account with the given values.
    /// Mainly used when loading data from JSON.
    /// </summary>
    /// <param name="id">Unique account ID.</param>
    /// <param name="name">Name of the account.</param>
    /// <param name="accountType">Type of account.</param>
    /// <param name="currency">Account currency.</param>
    /// <param name="balance">Current balance.</param>
    /// <param name="initialBalance">Initial balance when created.</param>
    /// <param name="lastUpdated">Last updated date.</param>
    /// <param name="transactions">List of transactions.</param>
    /// <param name="interestRate">Interest rate (for savings accounts).</param>
    [JsonConstructor]
    public BankAccount(
        Guid id,
        string name,
        AccountType accountType,
        CurrencyType currency,
        decimal balance,
        decimal initialBalance,
        DateTime lastUpdated,
        IReadOnlyList<Transaction>? transactions,
        DateTime? lastInterestApplied,
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
        LastInterestApplied = lastInterestApplied;
        _transactions = transactions != null ? new List<Transaction>(transactions) : new List<Transaction>();
    }

    /// <summary>
    /// Deposits a specified amount into the account and records the transaction.
    /// </summary>
    /// <param name="amount">The amount to deposit.</param>
    /// <param name="transactionId">Optional transaction ID (auto-generated if empty).</param>
    /// <param name="note">Optional note for the transaction.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown if the amount is less than or equal to zero.</exception>
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
    
    /// <summary>
    /// Withdraws a specified amount from the account and records the transaction.
    /// </summary>
    /// <param name="amount">The amount to withdraw.</param>
    /// <param name="transactionId">Optional transaction ID (auto-generated if empty).</param>
    /// <param name="note">Optional note for the transaction.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown if the amount is less than or equal to zero.</exception>
    /// <exception cref="InvalidOperationException">Thrown if there are insufficient funds.</exception>
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

    /// <summary>
    ///  Deposit and withdraw amount into the bank account
    /// </summary>
    public void Deposit(decimal amount, string? note = null) => Deposit(amount, Guid.NewGuid(), note ?? "Deposit");
    public void Withdraw(decimal amount, string? note = null) => Withdraw(amount, Guid.NewGuid(), note ?? "Withdrawal");

    /// <summary>
    ///  Adds an internal transaction to the account's transaction list.
    /// </summary>
    internal void AddInternalTransaction(Transaction tx) => _transactions.Add(tx);

    /// <summary>
    ///  Transfers a specified amount from this account to the target account and records the transaction.
    /// </summary>
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

    /// <summary>
    ///  Removes a transaction by its ID and updates the account balance accordingly.
    /// </summary>
    /// <param name="transactionId">The ID of the transaction to remove.</param>
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

    /// <summary>
    ///  Calculates and applies annual interest to the account if it is a savings account.
    ///  Button that applies interest to the account if it is a savings account (Sparkonto).
    ///  Also records the interest transaction.
    /// </summary>
    public void ApplyInterest()
    {
        // Only apply interest for savings accounts with a valid rate
        if (AccountType != AccountType.Sparkonto || !InterestRate.HasValue || InterestRate.Value <= 0m)
            return;

        var now = DateTime.UtcNow;

        var before = Balance;
        var interestAmount = Math.Round(Balance * InterestRate.Value, 2);

        // If interest is zero, do nothing
        if (interestAmount <= 0m)
            return;

        Balance += interestAmount;

        var tx = new Transaction
        {
            Id = Guid.NewGuid(),
            TimeStamp = now,
            Amount = interestAmount,
            ToAccountId = Id,
            TransactionType = TransactionType.Interest,
            Note = $"Ränta Sparkonto ({InterestRate.Value * 100:0}%)",
            BalanceBefore = before,
            BalanceAfter = Balance,
            ToAccountName = Name,
            LastUpdated = now
        };

        _transactions.Add(tx);
        LastInterestApplied = now;
        LastUpdated = now;
    }
}