using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Blazored.LocalStorage;
using Blazored.Toast.Services;
using Template.Shared.Dto;
using Template.Shared.Dto.Responses;
using Microsoft.AspNetCore.Components;

namespace Template.Web.Application.Services;

public interface IHttpService
{
    Task<T?> Get<T>(string uri, CancellationToken token = default);
    Task<T?> Post<T>(string uri, object? body = null, CancellationToken token = default);
    Task Post(string uri, object? body = null, CancellationToken token = default);
    Task PostFile(string uri, FileStream stream, string fileName, CancellationToken token = default);
    
    Task<T?> PostFile<T>(string uri, Microsoft.AspNetCore.Components.Forms.IBrowserFile file, CancellationToken token = default);
    Task<T?> Put<T>(string uri, object? body = null, CancellationToken token = default);
    Task Put(string uri, object? body = null, CancellationToken token = default);
    Task<T?> Patch<T>(string uri, object? body = null, CancellationToken token = default);
    Task Patch(string uri, object? body = null, CancellationToken token = default);
    Task<T?> Delete<T>(string uri, object? body = null, CancellationToken token = default);
    Task Delete(string uri, object? body = null, CancellationToken token = default);
}

public class HttpService : IHttpService
{
    private readonly HttpClient _httpClient;
    private readonly NavigationManager _navigationManager;
    private readonly ILocalStorageService _localStorageService;

    public HttpService(
        HttpClient httpClient,
        NavigationManager navigationManager,
        ILocalStorageService localStorageService)
    {
        _httpClient = httpClient;
        _navigationManager = navigationManager;
        _localStorageService = localStorageService;
    }

    public async Task<T?> Get<T>(string uri, CancellationToken token = default)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, uri);
        return await SendRequest<T>(request, token);
    }

    public async Task<T?> Post<T>(string uri, object? body = null, CancellationToken token = default)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, uri);
        if (body != null)
            request.Content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");
        return await SendRequest<T>(request, token);
    }
    public async Task Post(string uri, object? body = null, CancellationToken token = default)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, uri);
        if (body != null)
            request.Content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");
        await SendRequest(request, token);
    }

    public async Task PostFile(string uri, FileStream stream, string fileName, CancellationToken token = default)
    {
        using var ms = new MemoryStream();

        stream.Position = 0; // Ensure we're reading from the start of the file stream
        await stream.CopyToAsync(ms, token);
        var fileBytes = ms.ToArray();

        using var content = new MultipartFormDataContent();
        var byteContent = new ByteArrayContent(fileBytes);
        content.Add(byteContent, "file", fileName);

        var request = new HttpRequestMessage(HttpMethod.Post, uri) { Content = content };
        await SendRequest(request, token);
    }

    public async Task<T?> PostFile<T>(string uri, Microsoft.AspNetCore.Components.Forms.IBrowserFile file, CancellationToken token = default)
    {
        // 10MB file size limit
        const long maxFileSize = 10 * 1024 * 1024;

        using var ms = new MemoryStream();
        await file.OpenReadStream(maxFileSize).CopyToAsync(ms, token);
        ms.Position = 0;

        using var content = new MultipartFormDataContent();
        var byteContent = new ByteArrayContent(ms.ToArray());
        content.Add(byteContent, "file", file.Name);

        var request = new HttpRequestMessage(HttpMethod.Post, uri) { Content = content };
        return await SendRequest<T>(request, token);
    }

    public async Task<T?> Put<T>(string uri, object? body = null, CancellationToken token = default)
    {
        var request = new HttpRequestMessage(HttpMethod.Put, uri);
        if (body != null)
            request.Content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");
        return await SendRequest<T>(request, token);
    }
    public async Task Put(string uri, object? body = null, CancellationToken token = default)
    {
        var request = new HttpRequestMessage(HttpMethod.Put, uri);
        if (body != null)
            request.Content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");
        await SendRequest(request, token);
    }

    public async Task<T?> Patch<T>(string uri, object? body = null, CancellationToken token = default)
    {
        var request = new HttpRequestMessage(HttpMethod.Patch, uri);
        if (body != null)
            request.Content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");
        return await SendRequest<T>(request, token);
    }

    public async Task Patch(string uri, object? body = null, CancellationToken token = default)
    {
        var request = new HttpRequestMessage(HttpMethod.Patch, uri);
        if (body != null)
            request.Content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");
        await SendRequest(request, token);
    }

    public async Task<T?> Delete<T>(string uri, object? body = null, CancellationToken token = default)
    {
        var request = new HttpRequestMessage(HttpMethod.Delete, uri);
        if (body != null)
            request.Content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");
        return await SendRequest<T>(request, token);
    }
    public async Task Delete(string uri, object? body = null, CancellationToken token = default)
    {
        var request = new HttpRequestMessage(HttpMethod.Delete, uri);
        if (body != null)
            request.Content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");
        await SendRequest(request, token);
    }

    // helper methods

    private async Task<T?> SendRequest<T>(HttpRequestMessage request, CancellationToken cancellationToken = default)
    {
        // add jwt auth header if user is logged in and request is to the api url
        var token = await _localStorageService.GetItemAsync<AuthResponse>("user", cancellationToken);
        var isApiUrl = !request.RequestUri?.IsAbsoluteUri;
        if (token != null && isApiUrl != null && isApiUrl.Value)
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token.Token);

        using var response = await _httpClient.SendAsync(request, cancellationToken);

        // auto logout on 401 response
        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            _navigationManager.NavigateTo("login");
            return default!;
        }

        // 403 response
        if (response.StatusCode == HttpStatusCode.Forbidden)
        {
            throw new Exception("You do not have permission to access this resource.");
        }

        // 404 response
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            throw new Exception("Resource not found!");
        }

        // throw exception on error response
        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadFromJsonAsync<ErrorResponseDto>(cancellationToken: cancellationToken);
            throw new Exception(error?.Error);
        }

        if (typeof(T) == typeof(byte[]))
        {
            return (T)(object)await response.Content.ReadAsByteArrayAsync(cancellationToken);
        }
        return await response.Content.ReadFromJsonAsync<T>(cancellationToken: cancellationToken);
    }
    private async Task SendRequest(HttpRequestMessage request, CancellationToken cancellationToken = default)
    {
        // add jwt auth header if user is logged in and request is to the api url
        var token = await _localStorageService.GetItemAsync<AuthResponse>("user", cancellationToken);
        var isApiUrl = !request.RequestUri?.IsAbsoluteUri;
        if (token != null && isApiUrl != null && isApiUrl.Value)
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token.Token);

        using var response = await _httpClient.SendAsync(request, cancellationToken);

        if (response.StatusCode == HttpStatusCode.Unauthorized && token is not null && await TryRefreshToken(token))
        {
            // Retry the original request after refreshing the token
            token = await _localStorageService.GetItemAsync<AuthResponse>("user", cancellationToken);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token?.Token);
            await SendRequest(request, cancellationToken);
        }

        // auto logout on 401 response
        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            _navigationManager.NavigateTo("logout");
        }

        // throw exception on error response
        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadFromJsonAsync<Dictionary<string, string>>(cancellationToken: cancellationToken);
            throw new Exception(error?["message"]);
        }
    }

    private async Task<bool> TryRefreshToken(AuthResponse token)
    {
        var refreshTokenRequest = new TokenRequest
        {
            Token = token.Token,
            RefreshToken = token.RefreshToken
        };

        var refreshTokenContent = new StringContent(
            JsonSerializer.Serialize(refreshTokenRequest),
            Encoding.UTF8,
            "application/json");

        using var refreshTokenResponse = await _httpClient.PostAsync("/Admin/Identity/AdminAuth/RefreshToken", refreshTokenContent);

        if (refreshTokenResponse.IsSuccessStatusCode)
        {
            var newToken = await refreshTokenResponse.Content.ReadFromJsonAsync<AuthResponse>();
            await _localStorageService.SetItemAsync("user", newToken);
            return true;
        }

        return false;
    }
}
