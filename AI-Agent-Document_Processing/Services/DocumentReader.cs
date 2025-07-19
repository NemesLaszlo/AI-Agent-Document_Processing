using DocumentFormat.OpenXml.Packaging;
using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Canvas.Parser;
using Microsoft.Extensions.Logging;
using System.Text;
using DocumentFormat.OpenXml.Wordprocessing;

namespace AI_Agent_Document_Processing.Services
{
    public class DocumentReader : IDocumentReader
    {
        private readonly ILogger<DocumentReader> _logger;

        public DocumentReader(ILogger<DocumentReader> logger)
        {
            _logger = logger;
        }

        public async Task<string> ReadDocumentAsync(string filePath)
        {
            var fileInfo = new FileInfo(filePath);

            if (!fileInfo.Exists)
                throw new FileNotFoundException($"Document not found: {filePath}");

            string extension = Path.GetExtension(filePath).ToLowerInvariant();

            try
            {
                return extension switch
                {
                    ".txt" => await ReadTextFileAsync(filePath),
                    ".pdf" => await ReadPdfFileAsync(filePath),
                    ".docx" => await ReadDocxFileAsync(filePath),
                    _ => throw new NotSupportedException($"Unsupported file type: {extension}")
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error reading document {FilePath}", filePath);
                throw;
            }
        }

        private async Task<string> ReadTextFileAsync(string filePath)
        {
            return await File.ReadAllTextAsync(filePath, Encoding.UTF8);
        }

        private async Task<string> ReadPdfFileAsync(string filePath)
        {
            return await Task.Run(() =>
            {
                var text = new StringBuilder();

                using var pdfReader = new PdfReader(filePath);
                using var pdfDocument = new PdfDocument(pdfReader);

                for (int page = 1; page <= pdfDocument.GetNumberOfPages(); page++)
                {
                    var pageText = PdfTextExtractor.GetTextFromPage(pdfDocument.GetPage(page));
                    text.AppendLine(pageText);
                }

                return text.ToString();
            });
        }

        private async Task<string> ReadDocxFileAsync(string filePath)
        {
            return await Task.Run(() =>
            {
                using var document = WordprocessingDocument.Open(filePath, false);
                var body = document.MainDocumentPart?.Document?.Body;

                if (body == null)
                    return string.Empty;

                var text = new StringBuilder();
                foreach (var paragraph in body.Elements<Paragraph>())
                {
                    text.AppendLine(paragraph.InnerText);
                }

                return text.ToString();
            });
        }
    }
}
