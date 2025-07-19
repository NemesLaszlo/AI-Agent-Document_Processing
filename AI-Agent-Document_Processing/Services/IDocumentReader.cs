namespace AI_Agent_Document_Processing.Services
{
    public interface IDocumentReader
    {
        Task<string> ReadDocumentAsync(string filePath);
    }
}
