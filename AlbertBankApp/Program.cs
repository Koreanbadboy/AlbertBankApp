using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using AlbertBankApp;
using AlbertBankApp.Interfaces;
using AlbertBankApp.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// ÄNDRAT  från 'sp' till '_' för att undvika varning om oanvänd parameter
builder.Services.AddScoped(_ => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

builder.Services.AddScoped<IAccountService, AccountService>();
// ÄNDRAT Tillagd idag - Registrerar LocalStorageService så den kan injiceras i komponenter
// ÄNDRAT Registrerad som interface för att möjliggöra dependency injection i både History och NewTransaction
builder.Services.AddScoped<ILocalStorageService, LocalStorageService>();


await builder.Build().RunAsync();