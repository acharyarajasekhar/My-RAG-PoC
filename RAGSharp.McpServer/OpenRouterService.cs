using System;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using OpenRouter.NET;

public sealed class OpenRouterService
{
    private readonly ILogger<OpenRouterService> _logger;
    private readonly string _apiKey;

    public OpenRouterService(ILogger<OpenRouterService> logger)
    {
        _logger = logger;
        _apiKey = Environment.GetEnvironmentVariable("OPENROUTER_API_KEY") ?? string.Empty;
    }

    public async Task<bool> IsReachableAsync()
    {
        using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
        client.DefaultRequestHeaders.Add("Authorization", $"Bearer {_apiKey}");
        try
        {
            var response = await client.GetAsync("https://openrouter.ai/api/v1/auth/key");
            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("✅ OpenRouter API is reachable and API key is valid");
                return true;
            }
            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                _logger.LogError("❌ API key is invalid or expired");
            }
            else
            {
                _logger.LogWarning("⚠️ OpenRouter responded with: {Status}", response.StatusCode);
            }
        }
        catch (TaskCanceledException)
        {
            _logger.LogError("❌ Connection timeout – OpenRouter is not reachable");
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "❌ Network error while contacting OpenRouter");
        }
        return false;
    }

    public OpenRouterClient CreateClient() => new OpenRouterClient(apiKey: _apiKey);
}
