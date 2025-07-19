using AI_Agent_Document_Processing.Services;
using DocumentProcessingAgent.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace DocumentProcessingAgent
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var host = CreateHostBuilder(args).Build();
            var app = host.Services.GetRequiredService<DocumentProcessingApplication>();
            await app.RunAsync();
        }

        static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureServices((context, services) =>
                {
                    services.AddHttpClient<IOllamaService, OllamaService>();
                    services.AddSingleton<IDocumentReader, DocumentReader>();
                    services.AddSingleton<IDocumentProcessor, DocumentProcessor>();
                    services.AddSingleton<DocumentProcessingApplication>();
                    services.AddLogging();
                });
    }
}