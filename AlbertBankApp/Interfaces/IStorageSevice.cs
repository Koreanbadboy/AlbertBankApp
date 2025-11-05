namespace AlbertBankApp.Interfaces;

/// <summary>
/// Defines asynchronous methods for saving and retrieving data from a storage provider
/// </summary>
public interface IStorageSevice
{
        //Spara
        Task SetItemAsync<T>(string key, T value);
        //Hämta
        Task<T?> GetItemAsync<T>(string key);
}