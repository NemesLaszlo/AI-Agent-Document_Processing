namespace AI_Agent_Document_Processing.Models
{
    public class AppSettings
    {
        public string OllamaEndpoint { get; set; } = "http://localhost:11434/api/chat";
        public string ModelName { get; set; } = "tinyllama";
        public string SampleTextsDirectory { get; set; } = string.Empty;
        public string OutputDirectory { get; set; } = string.Empty;
        public int RequestTimeoutSeconds { get; set; } = 300;
        public int MaxRetries { get; set; } = 3;
    }
}
