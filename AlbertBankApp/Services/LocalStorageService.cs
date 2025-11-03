using AlbertBankApp.Interfaces;
using System.Text.Json;
using Microsoft.JSInterop;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace AlbertBankApp.Services;

/// <summary>
///  Implements methods for storing and retrieving data from the browser's local storage
/// </summary>
public class LocalStorageService : ILocalStorageService
{
    private readonly IJSRuntime _jsRuntime;
    private readonly ILogger<LocalStorageService> _logger;
    
    public LocalStorageService(IJSRuntime jsRuntime)
    {
        _jsRuntime = jsRuntime;
        _logger = _logger;
    }
    
    /// <summary>
    ///  Retrieves an item from local storage
    /// </summary>
    public async Task<T?> GetItemAsync<T>(string key)
    {
        var json = await _jsRuntime.InvokeAsync<string>("localStorage.getItem", key);
        if (string.IsNullOrEmpty(json))
        {
            _logger.LogDebug("Ingen data hittades för {key}", key);
            return default;
        }
        
        _logger.LogDebug("Hämtade datan från localStorage {key}", key);
        return JsonSerializer.Deserialize<T>(json);
    }
    
    /// <summary>
    ///  Saves an item to local storage
    /// </summary>
    /// <param name="key">The key under which the item will be stored in local storage</param>
    /// <param name="value">The value to store. It will be serialized to JSON</param>
    /// <typeparam name="T">The type of the object to store</typeparam>
    public async Task SetItemAsync<T>(string key, T value)
    {
        var json = JsonSerializer.Serialize(value);
        await _jsRuntime.InvokeVoidAsync("localStorage.setItem", key, json);
        _logger.LogDebug("Saved localStorage {key}", key);
    }
    
    /// <summary>
    ///  Removes an item from local storage
    /// </summary>
    /// <param name="key">The key of the item to remove from local storage</param>
    public async Task RemoveItemAsync(string key)
    {
        await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", key);
        _logger.LogDebug("Removed localStorage {key}", key);
    }
}