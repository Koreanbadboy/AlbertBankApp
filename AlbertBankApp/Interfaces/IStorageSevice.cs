namespace AlbertBankApp.Interfaces;

public interface IStorageSevice
{
        //Spara
        Task SetItemAsync<T>(string key, T value);
        //Hämta
        Task<T?> GetItemAsync<T>(string key);
}