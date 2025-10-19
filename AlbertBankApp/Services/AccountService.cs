using AlbertBankApp.Domain;
using AlbertBankApp.Interfaces;

namespace AlbertBankApp.Services;

public class AccountService : IAccountService
{
    private const string StorageKey = "BankAccounts";
    private readonly ILocalStorageService _storage;
    private List<BankAccount> _accounts=new();
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
            if (stored !=null)
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

    public async Task<BankAccount> CreateAccountAsync(string name, AccountType accountType, CurrencyType currency, decimal InitialBalance = 0)
    {
        await EnsureLoadedAsync();
        var newAccount = new BankAccount
        {
            Id = Guid.NewGuid(),
            Name = name,
            AccountType = accountType,
            Currency = currency,
            Balance = InitialBalance,
            Transactions = new List<Transaction>()
        };
        
        _accounts.Add(newAccount);
        await SaveAsync();
        return newAccount;
    }

    public async Task DepositAsync(Guid accountId, decimal amount, string? note = null)
    {
        await EnsureLoadedAsync();
        var account = _accounts.FirstOrDefault(a => a.Id == accountId)
            ??throw new InvalidOperationException("Kontot hittades inte");
        account.Deposit(amount, note);
        await SaveAsync();
    }

    public async Task WithdrawAsync(Guid accountId, decimal amount, string? note = null)
    {
        await EnsureLoadedAsync();
        var account = _accounts.FirstOrDefault(a => a.Id == accountId)
            ??throw new InvalidOperationException("Kontot hittades inte");
        account.Withdraw(amount, note);
        await SaveAsync();
    }

    public async Task<IReadOnlyList<Transaction>> GetTransactionsAsync(Guid accountId)
    {
        await EnsureLoadedAsync();
        var account = _accounts.FirstOrDefault(a => a.Id == accountId)
            ??throw new InvalidOperationException("Kontot hittades inte");
        return account.Transactions.AsReadOnly();
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
        
        if (fromAccount.Balance < amount)
            throw new InvalidOperationException("Otillräckliga medel på från-kontot.");
        
        fromAccount.Withdraw(amount, $"Överföring till {toAccount.Name}");
        toAccount.Deposit(amount, $"Överföring från {fromAccount.Name}");
        await SaveAsync();
    }
}
