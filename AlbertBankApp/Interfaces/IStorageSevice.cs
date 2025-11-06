namespace AlbertBankApp.Interfaces;

/// <summary>
/// Defines asynchronous methods for saving and retrieving data from a storage provider
/// </summary>
public interface IStorageSevice
{
        //Saves
        Task SetItemAsync<T>(string key, T value);
        //Loads
        Task<T?> GetItemAsync<T>(string key);
}