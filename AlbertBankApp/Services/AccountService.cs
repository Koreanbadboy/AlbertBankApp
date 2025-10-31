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
    public async Task<BankAccount> CreateAccountAsync(string name, AccountType accountType, CurrencyType currency, IReadOnlyList<Transaction> initialTransactions, decimal? interestRate = null)
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
        
        if (fromAccountId == toAccountId)
            throw new InvalidOperationException("Kan inte överföra till samma konto.");
        
        if (amount <= 0)
            throw new InvalidOperationException("Ange ett giltigt belopp större än 0.");
        
        await EnsureLoadedAsync();

        var fromAccount = _accounts.FirstOrDefault(a => a.Id == fromAccountId);
        var toAccount = _accounts.FirstOrDefault(a => a.Id == toAccountId);
        
        if (fromAccount is null)
            throw new InvalidOperationException("Från-kontot hittades inte.");
        if (toAccount is null)
            throw new InvalidOperationException("Till-kontot hittades inte.");
        
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

    private IAccountService _accountServiceImplementation;

    public Task<bool> ValidatePinAsync(string pin)
    {
        return Task.FromResult(pin == _correctPin);
    }

    /// <summary>
    ///  Changes the interest rate of a Sparkonto account
    /// </summary>
    /// <param name="accountId">The specific account for change of interest rate</param>
    /// <param name="delta">The change to apply to the current interest rate</param>
    /// <exception cref="InvalidOperationException">Thrown if the account cannot be found or if the account type is not</exception>
    public async Task ChangeInterestAsync(Guid accountId, decimal delta)
    {
        await EnsureLoadedAsync();

        var account = _accounts.FirstOrDefault(a => a.Id == accountId)
                      ?? throw new InvalidOperationException("Account not found.");

        if (account.AccountType != AccountType.Sparkonto)
            throw new InvalidOperationException("Interest can only be changed for Sparkonto accounts.");

        account.InterestRate ??= 0m;
        account.InterestRate = Math.Max(0m, account.InterestRate.Value + delta);

        // counts new balance based on initial balance and new interest rate
        account.Balance = account.InitialBalance * (1 + (account.InterestRate ?? 0));

        await SaveAsync();
    }

    /// <summary>
    ///  Applies interest to a Sparkonto account
    /// </summary>
    /// <param name="accountId">Specific account for applying of interest rate</param>
    public async Task ApplyInterestAsync(Guid accountId)
    {
        await EnsureLoadedAsync();
        var account = _accounts.FirstOrDefault(a => a.Id == accountId);
        if (account != null && account.InterestRate.HasValue)
        {
            account.Balance += account.Balance * account.InterestRate.Value;
            await SaveAsync();
        }
    }
}