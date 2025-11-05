namespace AlbertBankApp.Domain;

/// <summary>
/// Specifies the type of transaction: deposit, withdrawal, or transfer.
/// </summary>
public enum TransactionType
{
    Deposit,
    Withdrawal,
    Transfer,
}

/// <summary>
/// Represents a financial transaction within the banking application.
/// </summary>
public class Transaction
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public DateTime TimeStamp { get; set; } = DateTime.UtcNow;
    public decimal Amount { get; set; }
    public decimal? BalanceBefore { get; set; }
    public decimal? BalanceAfter { get; set; }
    public Guid? FromAccountId { get; set; }
    public Guid? ToAccountId { get; set; }
    public TransactionType TransactionType { get; set; }
    public string FromAccountName { get; set; } = string.Empty;
    public string ToAccountName { get; set; } = string.Empty;
    public string Note { get; set; } = string.Empty;
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
}