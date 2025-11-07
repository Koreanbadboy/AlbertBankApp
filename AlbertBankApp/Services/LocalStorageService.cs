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

    // Use consistent JSON options so serialization/deserialization works reliably
    private static readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter(System.Text.Json.JsonNamingPolicy.CamelCase) }
    };
    
    public LocalStorageService(IJSRuntime jsRuntime, ILogger<LocalStorageService> logger)
    {
        _jsRuntime = jsRuntime ?? throw new ArgumentNullException(nameof(jsRuntime));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
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
        return JsonSerializer.Deserialize<T>(json, _jsonOptions);
    }
    
    /// <summary>
    ///  Saves an item to local storage
    /// </summary>
    /// <param name="key">The key under which the item will be stored in local storage</param>
    /// <param name="value">The value to store. It will be serialized to JSON</param>
    /// <typeparam name="T">The type of the object to store</typeparam>
    public async Task SetItemAsync<T>(string key, T value)
    {
        var json = JsonSerializer.Serialize(value, _jsonOptions);
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