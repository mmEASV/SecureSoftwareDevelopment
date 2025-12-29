using Blazored.LocalStorage;
using Template.Shared.Dto;
using Microsoft.AspNetCore.Components;

namespace Template.Web.Application.Services;

public interface IAuthenticationService
{
    AuthResponse? User { get; }
    Task Initialize();
    Task Login(string username, string password, CancellationToken token);
    Task Logout();
}

public class AuthenticationService : IAuthenticationService
{
    private IHttpService _httpService;
    private NavigationManager _navigationManager;
    private ILocalStorageService _localStorageService;
    public AuthResponse? User { get; private set; }

    public AuthenticationService(
        IHttpService httpService,
        NavigationManager navigationManager,
        ILocalStorageService localStorageService
    )
    {
        _httpService = httpService;
        _navigationManager = navigationManager;
        _localStorageService = localStorageService;
    }

    public async Task Initialize()
    {
        User = await _localStorageService.GetItemAsync<AuthResponse>("user");
    }

    public async Task Login(string username, string password, CancellationToken token)
    {
        User = await _httpService.Post<AuthResponse>("/Identity/Auth/login", new LoginRequest { Email = username, Password = password }, token);
        await _localStorageService.SetItemAsync("user", User);
    }

    public async Task Logout()
    {
        User = null;
        await _localStorageService.RemoveItemAsync("user");
        _navigationManager.NavigateTo("login");
    }
}
