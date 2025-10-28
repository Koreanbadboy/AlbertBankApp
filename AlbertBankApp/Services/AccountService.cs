using AlbertBankApp.Domain;
using AlbertBankApp.Interfaces;

namespace AlbertBankApp.Services;

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

    private async Task SaveAsync()
    {
        await _storage.SetItemAsync(StorageKey, _accounts);
    }

    public async Task<IReadOnlyList<BankAccount>> GetAccountsAsync()
    {
        await EnsureLoadedAsync();
        return _accounts.AsReadOnly();
    }

    public async Task<BankAccount> CreateAccountAsync(string name, AccountType accountType, CurrencyType currency,
        decimal initialBalance = 0)
    {
        await EnsureLoadedAsync();
        var newAccount = new BankAccount(Guid.NewGuid(), name, accountType, currency, initialBalance);

        _accounts.Add(newAccount);
        await SaveAsync();
        return newAccount;
    }

    public async Task DepositAsync(Guid accountId, decimal amount, string? note = null)
    {
        await EnsureLoadedAsync();
        var account = _accounts.FirstOrDefault(a => a.Id == accountId)
                      ?? throw new InvalidOperationException("Kontot hittades inte");
        account.Deposit(amount, note);
        await SaveAsync();
    }

    public async Task WithdrawAsync(Guid accountId, decimal amount, string? note = null)
    {
        await EnsureLoadedAsync();
        var account = _accounts.FirstOrDefault(a => a.Id == accountId)
                      ?? throw new InvalidOperationException("Kontot hittades inte");
        account.Withdraw(amount, note);
        await SaveAsync();
    }

    public async Task<IReadOnlyList<Transaction>> GetTransactionsAsync(Guid accountId)
    {
        await EnsureLoadedAsync();
        var account = _accounts.FirstOrDefault(a => a.Id == accountId)
                      ?? throw new InvalidOperationException("Kontot hittades inte");
        return account.Transactions;
    }

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

    public async Task TransferAsync(Guid fromAccountId, Guid toAccountId, decimal amount)
    {
        if (fromAccountId == toAccountId)
            throw new InvalidOperationException("Kan inte överföra till samma konto.");

        if (amount <= 0)
            throw new ArgumentOutOfRangeException(nameof(amount), "Beloppet måste vara positivt.");

        await EnsureLoadedAsync();

        var fromAccount = _accounts.FirstOrDefault(a => a.Id == fromAccountId);
        var toAccount = _accounts.FirstOrDefault(a => a.Id == toAccountId);

        if (fromAccount == null) throw new KeyNotFoundException("Från-kontot hittades inte.");
        if (toAccount == null) throw new KeyNotFoundException("Till-kontot hittades inte.");

        // Use domain method that updates balances and creates a shared Transaction
        fromAccount.TransferTo(toAccount, amount);

        await SaveAsync();
    }

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

    public List<IBankAccount> GetAccounts()
    {
        return _accounts.Cast<IBankAccount>().ToList();
    }

    public async Task SaveAccountsAsync()
    {
        await _storage.SetItemAsync(StorageKey, _accounts);
    }
    
    /// <summary>
    /// 
    /// </summary>
    private readonly string _correctPin = "1234";
    public Task<bool> ValidatePinAsync(string pin)
    {
        return Task.FromResult(pin == _correctPin);
    }
}