using AI_Agent_Document_Processing.Models;
using AI_Agent_Document_Processing.Services;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace DocumentProcessingAgent.Services
{
    public class DocumentProcessor : IDocumentProcessor
    {
        private readonly IOllamaService _ollamaService;
        private readonly IDocumentReader _documentReader;
        private readonly ILogger<DocumentProcessor> _logger;
        private readonly AppSettings _settings;

        public DocumentProcessor(
            IOllamaService ollamaService,
            IDocumentReader documentReader,
            ILogger<DocumentProcessor> logger)
        {
            _ollamaService = ollamaService;
            _documentReader = documentReader;
            _logger = logger;
            _settings = LoadSettings();
        }

        public async Task<bool> TestConnectionAsync()
        {
            return await _ollamaService.TestConnectionAsync();
        }

        public async Task SummarizeDocumentAsync(List<DocumentInfo> documents)
        {
            var document = SelectSingleDocument(documents, "summarize");
            if (document == null) return;

            Console.WriteLine($"Processing: {document.FileName}");

            try
            {
                string content = await _documentReader.ReadDocumentAsync(document.FilePath);
                Console.WriteLine($"Loaded document with {content.Length} characters. Generating summary...");

                var summaryFormat = new
                {
                    type = "object",
                    properties = new
                    {
                        title = new { type = "string" },
                        summary = new { type = "string" },
                        keyPoints = new { type = "array", items = new { type = "string" } },
                        topics = new { type = "array", items = new { type = "string" } },
                        wordCount = new { type = "number" },
                        confidence = new { type = "number", minimum = 0, maximum = 1 }
                    },
                    required = new[] { "title", "summary", "keyPoints", "confidence" }
                };

                await ProcessStructuredPromptAsync(
                    $"Analyze and summarize the following document. Provide a confidence score (0-1) for your summary accuracy:\n\n{content}",
                    summaryFormat,
                    $"summary_{Path.GetFileNameWithoutExtension(document.FileName)}"
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error summarizing document {FileName}", document.FileName);
                Console.WriteLine($"Error processing document: {ex.Message}");
            }
        }

        public async Task CompareDocumentsAsync(List<DocumentInfo> documents)
        {
            if (documents.Count < 2)
            {
                Console.WriteLine("You need at least 2 documents to compare.");
                return;
            }

            var selectedDocs = SelectMultipleDocuments(documents, "compare");
            if (selectedDocs.Count < 2) return;

            var documentContents = new List<(string fileName, string content)>();

            foreach (var doc in selectedDocs)
            {
                try
                {
                    string content = await _documentReader.ReadDocumentAsync(doc.FilePath);
                    documentContents.Add((doc.FileName, content));
                    Console.WriteLine($"Loaded: {doc.FileName}");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error reading document {FileName}", doc.FileName);
                    Console.WriteLine($"Error reading {doc.FileName}: {ex.Message}");
                }
            }

            if (documentContents.Count < 2)
            {
                Console.WriteLine("Could not load enough documents for comparison.");
                return;
            }

            var comparisonFormat = new
            {
                type = "object",
                properties = new
                {
                    documents = new
                    {
                        type = "array",
                        items = new
                        {
                            type = "object",
                            properties = new
                            {
                                fileName = new { type = "string" },
                                mainTopic = new { type = "string" },
                                documentType = new { type = "string" }
                            }
                        }
                    },
                    commonalities = new { type = "array", items = new { type = "string" } },
                    differences = new { type = "array", items = new { type = "string" } },
                    similarityScore = new { type = "number", minimum = 0, maximum = 1 },
                    recommendation = new { type = "string" }
                },
                required = new[] { "commonalities", "differences", "similarityScore" }
            };

            string prompt = "Compare the following documents and provide detailed analysis:\n\n";
            for (int i = 0; i < documentContents.Count; i++)
            {
                prompt += $"Document {i + 1}: {documentContents[i].fileName}\n{documentContents[i].content}\n\n";
            }

            string outputBaseName = "comparison_" + string.Join("_vs_",
                selectedDocs.Take(2).Select(d => Path.GetFileNameWithoutExtension(d.FileName)));

            await ProcessStructuredPromptAsync(prompt, comparisonFormat, outputBaseName);
        }

        public async Task AnswerQuestionsAsync(List<DocumentInfo> documents)
        {
            var selectedDocs = SelectMultipleDocuments(documents, "query", allowSingle: true);
            if (selectedDocs.Count == 0) return;

            // Load all selected documents
            var documentContents = new Dictionary<string, string>();
            foreach (var doc in selectedDocs)
            {
                try
                {
                    string content = await _documentReader.ReadDocumentAsync(doc.FilePath);
                    documentContents[doc.FileName] = content;
                    Console.WriteLine($"Loaded: {doc.FileName}");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error reading document {FileName}", doc.FileName);
                    Console.WriteLine($"Error reading {doc.FileName}: {ex.Message}");
                }
            }

            if (documentContents.Count == 0)
            {
                Console.WriteLine("Could not load any documents.");
                return;
            }

            Console.WriteLine("\nDocument Q&A Session");
            Console.WriteLine("====================");
            Console.WriteLine("Enter your questions (type 'quit' to exit):");

            while (true)
            {
                Console.Write("\nQuestion: ");
                string? question = Console.ReadLine();
                if (string.IsNullOrEmpty(question) || question.ToLower() == "quit")
                    break;

                var qaFormat = new
                {
                    type = "object",
                    properties = new
                    {
                        answer = new { type = "string" },
                        confidence = new { type = "number", minimum = 0, maximum = 1 },
                        sourceDocuments = new { type = "array", items = new { type = "string" } },
                        relevantQuotes = new { type = "array", items = new { type = "string" } }
                    },
                    required = new[] { "answer", "confidence" }
                };

                string contextualPrompt = "Based on the following documents, answer the question accurately:\n\n";
                foreach (var doc in documentContents)
                {
                    contextualPrompt += $"Document: {doc.Key}\n{doc.Value}\n\n";
                }
                contextualPrompt += $"Question: {question}";

                await ProcessStructuredPromptAsync(contextualPrompt, qaFormat, null);
            }
        }

        public async Task ExtractEntitiesAsync(List<DocumentInfo> documents)
        {
            var document = SelectSingleDocument(documents, "extract entities from");
            if (document == null) return;

            Console.WriteLine($"Processing: {document.FileName}");

            try
            {
                string content = await _documentReader.ReadDocumentAsync(document.FilePath);
                Console.WriteLine($"Loaded document with {content.Length} characters. Extracting entities...");

                var entityFormat = new
                {
                    type = "object",
                    properties = new
                    {
                        people = new { type = "array", items = new { type = "string" } },
                        organizations = new { type = "array", items = new { type = "string" } },
                        locations = new { type = "array", items = new { type = "string" } },
                        dates = new { type = "array", items = new { type = "string" } },
                        numbers = new { type = "array", items = new { type = "string" } },
                        technologies = new { type = "array", items = new { type = "string" } },
                        keyTerms = new { type = "array", items = new { type = "string" } }
                    },
                    required = new[] { "people", "organizations", "locations" }
                };

                await ProcessStructuredPromptAsync(
                    $"Extract and categorize all named entities from the following document:\n\n{content}",
                    entityFormat,
                    $"entities_{Path.GetFileNameWithoutExtension(document.FileName)}"
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error extracting entities from document {FileName}", document.FileName);
                Console.WriteLine($"Error processing document: {ex.Message}");
            }
        }

        private DocumentInfo? SelectSingleDocument(List<DocumentInfo> documents, string action)
        {
            Console.WriteLine($"\nSelect a document to {action}:");
            Console.Write("Enter the document number: ");

            if (!int.TryParse(Console.ReadLine(), out int fileIndex) || fileIndex < 1 || fileIndex > documents.Count)
            {
                Console.WriteLine("Invalid selection.");
                return null;
            }

            return documents[fileIndex - 1];
        }

        private List<DocumentInfo> SelectMultipleDocuments(List<DocumentInfo> documents, string action, bool allowSingle = false)
        {
            Console.WriteLine($"\nSelect documents to {action}:");
            Console.WriteLine("Enter document numbers (comma-separated, e.g., 1,2,3):");

            Console.Write("Selection: ");
            string? input = Console.ReadLine();

            var selections = input?.Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(s => s.Trim())
                .ToArray() ?? Array.Empty<string>();

            var selectedDocs = new List<DocumentInfo>();

            foreach (string selection in selections)
            {
                if (int.TryParse(selection, out int index) && index >= 1 && index <= documents.Count)
                {
                    selectedDocs.Add(documents[index - 1]);
                }
            }

            if (selectedDocs.Count == 0 || (!allowSingle && selectedDocs.Count < 2))
            {
                Console.WriteLine($"Invalid selection. You need to select at least {(allowSingle ? 1 : 2)} document(s).");
                return new List<DocumentInfo>();
            }

            return selectedDocs;
        }

        private async Task ProcessStructuredPromptAsync(string prompt, object format, string? outputBaseName)
        {
            Console.WriteLine("\nSending request to AI model...");

            try
            {
                var response = await _ollamaService.SendStructuredRequestAsync(prompt, format);

                if (!string.IsNullOrEmpty(response))
                {
                    var cleanedJson = CleanJsonString(response);
                    var structuredData = JsonSerializer.Deserialize<JsonElement>(cleanedJson);

                    if (!string.IsNullOrEmpty(outputBaseName))
                    {
                        await SaveAnalysisAsync(structuredData, outputBaseName);
                    }

                    Console.WriteLine("\nAI Response:");
                    Console.WriteLine(JsonSerializer.Serialize(structuredData, new JsonSerializerOptions { WriteIndented = true }));
                }
                else
                {
                    Console.WriteLine("No response received from the AI model.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing structured prompt");
                Console.WriteLine($"Error: {ex.Message}");
            }
        }

        private async Task SaveAnalysisAsync(JsonElement data, string baseName)
        {
            try
            {
                string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                string fileName = $"{baseName}_{timestamp}.json";
                string filePath = Path.Combine(_settings.OutputDirectory, fileName);

                string json = JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });
                await File.WriteAllTextAsync(filePath, json);

                Console.WriteLine($"Analysis saved to: {filePath}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving analysis");
                Console.WriteLine($"Could not save analysis: {ex.Message}");
            }
        }

        private static string CleanJsonString(string rawContent)
        {
            if (string.IsNullOrEmpty(rawContent))
                return "{}";

            rawContent = rawContent.Trim();

            if (rawContent.StartsWith("{") && rawContent.EndsWith("}"))
            {
                return rawContent;
            }
            else if (rawContent.Contains("{") && rawContent.Contains("}"))
            {
                int startIndex = rawContent.IndexOf("{");
                int endIndex = rawContent.LastIndexOf("}");
                if (startIndex < endIndex)
                {
                    return rawContent.Substring(startIndex, endIndex - startIndex + 1);
                }
            }

            return "{}";
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
