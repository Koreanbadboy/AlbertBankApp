using AlbertBankApp.Interfaces;
using System.Text.Json;
using Microsoft.JSInterop;
using System.Threading.Tasks;

namespace AlbertBankApp.Services;

/// <summary>
///  Implements methods for storing and retrieving data from the browser's local storage
/// </summary>
public class LocalStorageService : ILocalStorageService
{
    private readonly IJSRuntime _jsRuntime;
    
    public LocalStorageService(IJSRuntime jsRuntime)
    {
        _jsRuntime = jsRuntime;
    }
    public async Task<T?> GetItemAsync<T>(string key)
    {
        var json = await _jsRuntime.InvokeAsync<string>("localStorage.getItem", key);
        if (string.IsNullOrEmpty(json))
        {
            return default;
        }
        
        return JsonSerializer.Deserialize<T>(json);
    }
    
    /// <summary>
    ///  Saves an item to local storage
    /// </summary>
    /// <param name="key"></param>
    /// <param name="value"></param>
    /// <typeparam name="T"></typeparam>
    public async Task SetItemAsync<T>(string key, T value)
    {
        var json = JsonSerializer.Serialize(value);
        await _jsRuntime.InvokeVoidAsync("localStorage.setItem", key, json);
    }
    
    /// <summary>
    ///  Removes an item from local storage
    /// </summary>
    /// <param name="key"></param>
    public async Task RemoveItemAsync(string key)
    {
        await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", key);
    }
}