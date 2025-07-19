namespace AI_Agent_Document_Processing.Services
{
    public interface IOllamaService
    {
        Task<bool> TestConnectionAsync();
        Task<string> SendStructuredRequestAsync(string prompt, object format);
    }
}
