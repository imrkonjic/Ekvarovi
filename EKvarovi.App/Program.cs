using System.Globalization;
using Blazored.LocalStorage;
using EKvarovi.App;
using EKvarovi.App.Services;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using MudBlazor.Services;

var hr = new CultureInfo("hr-HR");
CultureInfo.DefaultThreadCurrentCulture = hr;
CultureInfo.DefaultThreadCurrentUICulture = hr;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddMudServices();
builder.Services.AddBlazoredLocalStorage();
builder.Services.AddScoped<JwtAuthStateProvider>();
builder.Services.AddScoped<AuthenticationStateProvider>(sp =>
    sp.GetRequiredService<JwtAuthStateProvider>());
builder.Services.AddAuthorizationCore();
builder.Services.AddTransient<AuthHeaderHandler>();
builder.Services.AddHttpClient<ApiClient>(c =>
        c.BaseAddress = new Uri(builder.Configuration["ApiBaseUrl"]!))
    .AddHttpMessageHandler<AuthHeaderHandler>();
builder.Services.AddHttpClient("Api", c =>
        c.BaseAddress = new Uri(builder.Configuration["ApiBaseUrl"]!))
    .AddHttpMessageHandler<AuthHeaderHandler>();
builder.Services.AddScoped<LookupCache>();
builder.Services.AddScoped<IAttachmentMediaService, AttachmentMediaService>();

await builder.Build().RunAsync();
