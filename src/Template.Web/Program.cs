using Blazored.Modal;
using Blazored.Toast;
using Template.Web;
using Template.Web.Application.Extension;
using Template.Web.Application.Services;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.FluentUI.AspNetCore.Components;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddBlazorBootstrap();
builder.Services.AddBlazoredToast();
builder.Services.AddBlazoredModal();
builder.Services.AddFluentUIComponents();

builder.Services.AddServicesAndRepositories();

var globalConfig = builder.Configuration.GetSection("Global");

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(globalConfig.GetValue<string>("ApiUrl") ?? throw new NullReferenceException("ApiUrl cannot be null")) });

var host = builder.Build();

var authenticationService = host.Services.GetRequiredService<IAuthenticationService>();
await authenticationService.Initialize();

await host.RunAsync();
