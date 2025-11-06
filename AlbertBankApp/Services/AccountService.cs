using AlbertBankApp.Domain;
using AlbertBankApp.Interfaces;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace AlbertBankApp.Services;

/// <summary>
/// Service for managing bank accounts
/// </summary>
public class AccountService : IAccountService
{
    private readonly ILogger<AccountService> _logger; // ilogger
    private const string StorageKey = "BankAccounts";
    private readonly ILocalStorageService _storage;
    private List<BankAccount> _accounts = new();
    private bool _loaded;

    public AccountService(ILocalStorageService storage,  ILogger<AccountService> logger)
    {
        _storage = storage;
        _logger = logger;
    }
    
   
    private async Task EnsureLoadedAsync()
    {
        if (_loaded) return;
        var fromStorage = await _storage.GetItemAsync<List<BankAccount>>(StorageKey);
        _accounts.Clear();
        if (fromStorage is { Count: > 0 })
        {
            _accounts.AddRange(fromStorage);
            _logger.LogInformation("Loaded {Count} accounts from storage.", fromStorage.Count);
        }
        else
        {
            _logger.LogInformation("No accounts found in storage.");
        }

        var now = DateTime.UtcNow;
        var anyApplied = false;
        foreach (var account in _accounts.Where(a => a.AccountType == AccountType.Sparkonto && a.InterestRate.HasValue && a.InterestRate.Value > 0))
        {
            var yearsElapsed = (int)((now - account.LastUpdated).TotalDays / 365);
            for (int i = 0; i < yearsElapsed; i++)
            {
                account.ApplyInterest();
                anyApplied = true;
            }
            account.LastUpdated = now;
        }
        if (anyApplied)
        {
            await SaveAsync();
        }
        _loaded = true;
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

        decimal? normalizedInterest = null;
        if (accountType == AccountType.Sparkonto)
        {
            normalizedInterest = interestRate.HasValue
                ? (interestRate.Value > 1m ? interestRate.Value / 100m : interestRate.Value)
                : 0.01m;
        }

        var newAccount = new BankAccount(
            Guid.NewGuid(),
            name,
            accountType,
            currency,
            initialBalance,
            initialBalance,
            DateTime.UtcNow,
            new List<Transaction>(),
            normalizedInterest
        );

        _accounts.Add(newAccount);
        _logger.LogInformation("Skapade nytt konto: {@Account}", newAccount);
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
        _logger.LogInformation("Skapade nytt konto: {@Account}", account);
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
        if (amount <= 0)
        {
            _logger.LogWarning("Ogiltigt uttagsbelopp : {@Amount}", amount);
        }
        
        account.Withdraw(amount, note);
        await SaveAsync();
        
        _logger.LogInformation("Uttag på {Amount} SEK från konto {AccountId}. Nytt saldo: {Balance} SEK",
            amount, account.Id, account.Balance);
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
            _logger.LogInformation("Tog borg konto: {@AccountId}({AccountName})", account.Id, account.Name);
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
    /// JSON options for export/import
    /// </summary>
    private static readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
    };
    
    /// <summary>
    /// JSON Export
    /// </summary>
    /// <returns></returns>
    public async Task<string> ExportJsonAsync()
    {
        await EnsureLoadedAsync();
        return JsonSerializer.Serialize(_accounts, _jsonOptions);
    }

    /// <summary>
    /// JSON Import
    /// </summary>
    /// <param name="json"></param>
    /// <param name="replaceExisting"></param>
    /// <returns></returns>
    public async Task<List<string>> ImportJsonAsync(string json, bool replaceExisting = false)
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(json))
        {
            errors.Add("Tom JSON.");
            return errors;
        }

        List<BankAccount>? incoming;
        try
        {
            incoming = JsonSerializer.Deserialize<List<BankAccount>>(json, _jsonOptions);
        }
        catch
        {
            errors.Add("Ogiltig JSON.");
            return errors;
        }

        if (incoming is null || incoming.Count == 0)
        {
            errors.Add("Ingen data.");
            return errors;
        }

        await EnsureLoadedAsync();

        if (replaceExisting)
        {
            _accounts = incoming.ToList();
        }
        else
        {
            var existing = _accounts.Select(a => a.Id).ToHashSet();
            foreach (var a in incoming)
                if (!existing.Contains(a.Id))
                    _accounts.Add(a);
        }

        await SaveAsync();
        return errors;
    }
    
    public async Task ApplyAnnualInterestAsync()
    {
        await EnsureLoadedAsync();
        foreach (var account in _accounts.Where(a => a.AccountType == AccountType.Sparkonto))
        {
            account.ApplyInterest();
            _logger.LogInformation("Applied annual interest to account {AccountId}. New balance: {Balance}", account.Id, account.Balance);
        }
        await SaveAsync();
    }
}
