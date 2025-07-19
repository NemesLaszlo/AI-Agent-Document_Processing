using AI_Agent_Document_Processing.Models;
using DocumentProcessingAgent.Services;
using Microsoft.Extensions.Logging;

namespace DocumentProcessingAgent
{
    public class DocumentProcessingApplication
    {
        private readonly ILogger<DocumentProcessingApplication> _logger;
        private readonly IDocumentProcessor _documentProcessor;
        private readonly AppSettings _settings;

        public DocumentProcessingApplication(
            ILogger<DocumentProcessingApplication> logger,
            IDocumentProcessor documentProcessor)
        {
            _logger = logger;
            _documentProcessor = documentProcessor;
            _settings = LoadSettings();
        }

        public async Task RunAsync()
        {
            try
            {
                _logger.LogInformation("Starting Document Processing Agent");
                Console.WriteLine("Document Processing Agent - Production Edition");
                Console.WriteLine("================================================");
                Console.WriteLine($"Using Ollama with model: {_settings.ModelName}");
                Console.WriteLine();

                if (!await _documentProcessor.TestConnectionAsync())
                {
                    Console.WriteLine("\nPress any key to exit...");
                    Console.ReadKey();
                    return;
                }

                EnsureDirectoriesExist();
                await RunMainMenuAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Application failed with error");
                Console.WriteLine($"Application error: {ex.Message}");
            }
        }

        private async Task RunMainMenuAsync()
        {
            var documentFiles = GetSupportedDocuments();

            if (documentFiles.Count == 0)
            {
                Console.WriteLine($"No supported documents found in {_settings.SampleTextsDirectory}");
                Console.WriteLine("Supported formats: .txt, .pdf, .docx");
                return;
            }

            DisplayDocuments(documentFiles);

            bool exitRequested = false;
            while (!exitRequested)
            {
                DisplayMenu();
                Console.Write("\nSelect an option (1-5): ");
                string? choice = Console.ReadLine();

                try
                {
                    switch (choice)
                    {
                        case "1":
                            await _documentProcessor.SummarizeDocumentAsync(documentFiles);
                            break;
                        case "2":
                            await _documentProcessor.CompareDocumentsAsync(documentFiles);
                            break;
                        case "3":
                            await _documentProcessor.AnswerQuestionsAsync(documentFiles);
                            break;
                        case "4":
                            await _documentProcessor.ExtractEntitiesAsync(documentFiles);
                            break;
                        case "5":
                            exitRequested = true;
                            Console.WriteLine("\nExiting application. Goodbye!");
                            break;
                        default:
                            Console.WriteLine("\nInvalid choice. Please enter a number between 1 and 5.");
                            break;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing user request");
                    Console.WriteLine($"An error occurred: {ex.Message}");
                }
            }
        }

        private static void DisplayMenu()
        {
            Console.WriteLine();
            Console.WriteLine("Select an operation:");
            Console.WriteLine("1. Summarize a single document");
            Console.WriteLine("2. Compare multiple documents");
            Console.WriteLine("3. Ask questions about documents");
            Console.WriteLine("4. Extract entities from documents");
            Console.WriteLine("5. Exit");
        }

        private static void DisplayDocuments(List<DocumentInfo> documents)
        {
            Console.WriteLine($"Found {documents.Count} supported document(s):");
            for (int i = 0; i < documents.Count; i++)
            {
                var doc = documents[i];
                Console.WriteLine($"{i + 1}. {doc.FileName} ({doc.Type}, {doc.Size:N0} bytes)");
            }
        }

        private List<DocumentInfo> GetSupportedDocuments()
        {
            var supportedExtensions = new[] { ".txt", ".pdf", ".docx" };
            var documents = new List<DocumentInfo>();

            if (!Directory.Exists(_settings.SampleTextsDirectory))
                return documents;

            var files = Directory.GetFiles(_settings.SampleTextsDirectory, "*.*", SearchOption.TopDirectoryOnly)
                .Where(file => supportedExtensions.Contains(Path.GetExtension(file).ToLowerInvariant()))
                .ToArray();

            foreach (var file in files)
            {
                var fileInfo = new FileInfo(file);
                documents.Add(new DocumentInfo
                {
                    FilePath = file,
                    FileName = fileInfo.Name,
                    Size = fileInfo.Length,
                    Type = Path.GetExtension(file).ToUpperInvariant().TrimStart('.'),
                    LastModified = fileInfo.LastWriteTime
                });
            }

            return documents;
        }

        private void EnsureDirectoriesExist()
        {
            if (!Directory.Exists(_settings.SampleTextsDirectory))
            {
                Console.WriteLine($"SampleTexts directory not found at: {_settings.SampleTextsDirectory}");
                Console.WriteLine("Please make sure the SampleTexts directory exists with your documents.");
            }

            if (!Directory.Exists(_settings.OutputDirectory))
            {
                Directory.CreateDirectory(_settings.OutputDirectory);
                Console.WriteLine($"Created directory for analysis output at: {_settings.OutputDirectory}");
            }
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
