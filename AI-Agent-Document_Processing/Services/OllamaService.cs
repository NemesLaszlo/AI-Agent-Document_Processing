using AI_Agent_Document_Processing.Models;
using Microsoft.Extensions.Logging;
using System.Text;
using System.Text.Json;

namespace AI_Agent_Document_Processing.Services
{
    public class OllamaService : IOllamaService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<OllamaService> _logger;
        private readonly AppSettings _settings;

        public OllamaService(HttpClient httpClient, ILogger<OllamaService> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
            _settings = LoadSettings();
            _httpClient.Timeout = TimeSpan.FromSeconds(_settings.RequestTimeoutSeconds);
        }

        public async Task<bool> TestConnectionAsync()
        {
            Console.WriteLine($"Testing connection to Ollama with model: {_settings.ModelName}");

            for (int attempt = 1; attempt <= _settings.MaxRetries; attempt++)
            {
                try
                {
                    var requestBody = new
                    {
                        model = _settings.ModelName,
                        messages = new[] { new { role = "user", content = "Test connection. Reply with 'OK'." } },
                        stream = false
                    };

                    string jsonRequest = JsonSerializer.Serialize(requestBody);
                    var content = new StringContent(jsonRequest, Encoding.UTF8, "application/json");

                    var response = await _httpClient.PostAsync(_settings.OllamaEndpoint, content);
                    response.EnsureSuccessStatusCode();

                    Console.WriteLine("Successfully connected to Ollama!");
                    return true;
                }
                catch (HttpRequestException ex) when (attempt < _settings.MaxRetries)
                {
                    _logger.LogWarning("Connection attempt {Attempt} failed: {Error}", attempt, ex.Message);
                    await Task.Delay(2000 * attempt);
                }
                catch (HttpRequestException ex)
                {
                    _logger.LogError(ex, "Failed to connect to Ollama after {MaxRetries} attempts", _settings.MaxRetries);
                    Console.WriteLine($"Error connecting to Ollama: {ex.Message}");
                    Console.WriteLine("\nPossible solutions:");
                    Console.WriteLine("1. Ensure Ollama is running: 'ollama serve'");
                    Console.WriteLine($"2. Install the model: 'ollama pull {_settings.ModelName}'");
                    Console.WriteLine($"3. Check the endpoint: {_settings.OllamaEndpoint}");
                    return false;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Unexpected error testing connection");
                    Console.WriteLine($"Unexpected error: {ex.Message}");
                    return false;
                }
            }
            return false;
        }

        public async Task<string> SendStructuredRequestAsync(string prompt, object format)
        {
            for (int attempt = 1; attempt <= _settings.MaxRetries; attempt++)
            {
                try
                {
                    var requestBody = new
                    {
                        model = _settings.ModelName,
                        messages = new[] { new { role = "user", content = prompt } },
                        stream = false,
                        format = format
                    };

                    string jsonRequest = JsonSerializer.Serialize(requestBody);
                    var content = new StringContent(jsonRequest, Encoding.UTF8, "application/json");

                    var response = await _httpClient.PostAsync(_settings.OllamaEndpoint, content);
                    response.EnsureSuccessStatusCode();

                    string jsonResponse = await response.Content.ReadAsStringAsync();
                    var parsedResponse = JsonSerializer.Deserialize<OllamaResponse>(jsonResponse);

                    return parsedResponse?.Message?.Content ?? string.Empty;
                }
                catch (Exception ex) when (attempt < _settings.MaxRetries)
                {
                    _logger.LogWarning("Request attempt {Attempt} failed: {Error}", attempt, ex.Message);
                    await Task.Delay(1000 * attempt);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to send request after {MaxRetries} attempts", _settings.MaxRetries);
                    throw;
                }
            }

            return string.Empty;
        }

        private AppSettings LoadSettings()
        {
            return new AppSettings
            {
                OllamaEndpoint = Environment.GetEnvironmentVariable("OLLAMA_ENDPOINT") ?? "http://localhost:11434/api/chat",
                ModelName = Environment.GetEnvironmentVariable("OLLAMA_MODEL") ?? "tinyllama",
                SampleTextsDirectory = Path.Combine(Directory.GetCurrentDirectory(), "SampleTexts"),
                OutputDirectory = Path.Combine(Directory.GetCurrentDirectory(), "Analysis"),
                RequestTimeoutSeconds = 300,
                MaxRetries = 3
            };
        }
    }
}
