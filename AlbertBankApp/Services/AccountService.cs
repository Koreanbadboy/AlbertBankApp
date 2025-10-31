using AlbertBankApp.Domain;
using AlbertBankApp.Interfaces;

namespace AlbertBankApp.Services;

/// <summary>
/// Service for managing bank accounts
/// </summary>
public class AccountService : IAccountService
{
    private const string StorageKey = "BankAccounts";
    private readonly ILocalStorageService _storage;
    private List<BankAccount> _accounts = new();
    private bool _loaded;

    public AccountService(ILocalStorageService storage)
    {
        _storage = storage;
    }

    /// <summary>
    /// Ensures that accounts are loaded from storage
    /// </summary>
    private async Task EnsureLoadedAsync()
    {
        if (!_loaded)
        {
            var stored = await _storage.GetItemAsync<List<BankAccount>>(StorageKey);
            if (stored != null)
                _accounts = stored;
            _loaded = true;
        }
    }

    /// <summary>
    ///  Saves accounts to storage
    /// </summary>
    private async Task SaveAsync()
    {
        await _storage.SetItemAsync(StorageKey, _accounts);
    }

    /// <summary>
    ///  Gets all bank accounts
    /// </summary>
    public async Task<IReadOnlyList<BankAccount>> GetAccountsAsync()
    {
        await EnsureLoadedAsync();
        return _accounts.AsReadOnly();
    }

    /// <summary>
    ///  Creates a new bank account with an initial balance
    /// </summary>
    public async Task<BankAccount> CreateAccountAsync(string name, AccountType accountType, CurrencyType currency,
        decimal initialBalance = 0, decimal? interestRate = null)
    {
        await EnsureLoadedAsync();
        var transactions = new List<Transaction>();
        var newAccount = new BankAccount(
            Guid.NewGuid(),
            name,
            accountType,
            currency,
            transactions,
            initialBalance,
            accountType == AccountType.Sparkonto ? (interestRate ?? 0) : null
        );
        _accounts.Add(newAccount);
        await SaveAsync();
        return newAccount;
    }

    /// <summary>
    ///  Creates a new bank account with initial transactions (needs only if importing with Json)
    /// </summary>
    public async Task<BankAccount> CreateAccountAsync(string name, AccountType accountType, CurrencyType currency,
        IReadOnlyList<Transaction> initialTransactions, decimal? interestRate = null)
    {
        await EnsureLoadedAsync();
        var newAccount = new BankAccount(
            Guid.NewGuid(),
            name,
            accountType,
            currency,
            initialTransactions,
            initialTransactions.Sum(t => t.Amount),
            accountType == AccountType.Sparkonto ? (interestRate ?? 0) : null
        );
        _accounts.Add(newAccount);
        await SaveAsync();
        return newAccount;
    }

    /// <summary>
    ///  Deposits an amount into a bank account
    /// </summary>
    public async Task DepositAsync(Guid accountId, decimal amount, string? note = null)
    {
        await EnsureLoadedAsync();
        var account = _accounts.FirstOrDefault(a => a.Id == accountId)
                      ?? throw new InvalidOperationException("Kontot hittades inte");
        account.Deposit(amount, note);
        await SaveAsync();
    }

    /// <summary>
    /// Withdraws an amount from a bank account
    /// </summary>
    /// <exception cref="InvalidOperationException">If the accounts could not be found</exception>
    public async Task WithdrawAsync(Guid accountId, decimal amount, string? note = null)
    {
        await EnsureLoadedAsync();
        var account = _accounts.FirstOrDefault(a => a.Id == accountId)
                      ?? throw new InvalidOperationException("Kontot hittades inte");
        account.Withdraw(amount, note);
        await SaveAsync();
    }

    /// <summary>
    ///  Gets transactions for a specific bank account in history page
    /// </summary>
    /// <exception cref="InvalidOperationException">If the accounts could not be found</exception>
    public async Task<IReadOnlyList<Transaction>> GetTransactionsAsync(Guid accountId)
    {
        await EnsureLoadedAsync();
        var account = _accounts.FirstOrDefault(a => a.Id == accountId)
                      ?? throw new InvalidOperationException("Kontot hittades inte");
        return account.Transactions;
    }

    /// <summary>
    ///  Deletes a bank account
    /// </summary>
    public async Task DeleteAccountAsync(Guid accountId)
    {
        await EnsureLoadedAsync();
        var account = _accounts.FirstOrDefault(a => a.Id == accountId);
        if (account != null)
        {
            _accounts.Remove(account);
            await SaveAsync();
        }
    }

    /// /// <summary>
    /// Transfers a specified amount of money from one bank account to another.
    /// </summary>
    /// <param name="fromAccountId">The unique identifier of the source account.</param>
    /// <param name="toAccountId">The unique identifier of the destination account.</param>
    /// <param name="amount">The amount of money to transfer. Must be greater than zero.</param>
    /// <exception cref="InvalidOperationException">Thrown if either account cannot be found, or if the source account has insufficient funds.</exception>
    public async Task TransferAsync(Guid fromAccountId, Guid toAccountId, decimal amount)
    {
        if (fromAccountId == Guid.Empty)
            throw new InvalidOperationException("Välj ett från-konto.");

        if (toAccountId == Guid.Empty)
            throw new InvalidOperationException("Välj ett till-konto.");

        if (amount <= 0)
            throw new InvalidOperationException("Ange ett giltigt belopp större än 0.");

        await EnsureLoadedAsync();

        var fromAccount = _accounts.FirstOrDefault(a => a.Id == fromAccountId);
        var toAccount = _accounts.FirstOrDefault(a => a.Id == toAccountId);

        if (fromAccount.Balance < amount)
            throw new InvalidOperationException("Otillräckligt saldo på från-kontot.");
        fromAccount.TransferTo(toAccount, amount);

        await SaveAsync();
    }

    /// <summary>
    ///  Deletes a specific transaction by its ID
    /// </summary>
    /// <param name="txId">The specific transaction to deletee</param>
    public async Task DeleteTransactionAsync(Guid txId)
    {
        await EnsureLoadedAsync();
        foreach (var account in _accounts)
        {
            if (account.Transactions.Any(t => t.Id == txId))
            {
                account.RemoveTransaction(txId);
                await SaveAsync();
                return;
            }
        }
    }

    /// <summary>
    ///  Gets all bank accounts as IBankAccount
    /// </summary>
    public List<IBankAccount> GetAccounts()
    {
        return _accounts.Cast<IBankAccount>().ToList();
    }

    /// <summary>
    ///  Saves accounts to storage
    /// </summary>
    public async Task SaveAccountsAsync()
    {
        await _storage.SetItemAsync(StorageKey, _accounts);
    }

    /// <summary>
    ///  Validates the provided PIN code
    /// </summary>
    private readonly string _correctPin = "1234";

    public Task<bool> ValidatePinAsync(string pin)
    {
        return Task.FromResult(pin == _correctPin);
    }
    
    /// <summary>
    ///  Adjusts the balance of a savings account by 1% and modifies the interest rate accordingly
    /// </summary>
    /// <param name="accountId">Specific account-id</param>
    /// <param name="increase">Adjust interest rate</param>
    public async Task AdjustBalanceByPercentAsync(Guid accountId, bool increase)
    {
        await EnsureLoadedAsync();

        var account = _accounts.FirstOrDefault(a => a.Id == accountId);
        if (account == null || account.AccountType != AccountType.Sparkonto)
            return;

        // Counting 1 percent of the current balance
        decimal percent = account.Balance * 0.01m;

        // increase or decease the balance
        if (increase)
            account.Deposit(percent, "Ökning med 1 %");
        else
            account.Withdraw(percent, "Minskning med 1 %");

        // adjust the interest rate by 1 %
        if (account.InterestRate == null)
            account.InterestRate = 0m;

        if (increase)
            account.InterestRate += 0.01m;
        else
            account.InterestRate -= 0.01m;

        // the interest rate must be between 0 % and 100 %
        if (account.InterestRate < 0m)
            account.InterestRate = 0m;
        if (account.InterestRate > 1m)
            account.InterestRate = 1m;

        await SaveAsync();
    }
}