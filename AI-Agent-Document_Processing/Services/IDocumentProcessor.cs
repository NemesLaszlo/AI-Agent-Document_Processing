using AI_Agent_Document_Processing.Models;

namespace DocumentProcessingAgent.Services
{
    public interface IDocumentProcessor
    {
        Task<bool> TestConnectionAsync();
        Task SummarizeDocumentAsync(List<DocumentInfo> documents);
        Task CompareDocumentsAsync(List<DocumentInfo> documents);
        Task AnswerQuestionsAsync(List<DocumentInfo> documents);
        Task ExtractEntitiesAsync(List<DocumentInfo> documents);
    }
}
