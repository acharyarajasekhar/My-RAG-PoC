using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using RAGSharp.IO;
using RAGSharp.RAG;

public sealed class DocumentLoader
{
    private readonly ILogger<DocumentLoader> _logger;
    private const string DocsPath = "./Data/Docs";

    public DocumentLoader(ILogger<DocumentLoader> logger) => _logger = logger;

    public async Task LoadAsync(RagRetriever retriever)
    {
        var loader = new DirectoryLoader();
        var docs = await loader.LoadAsync(DocsPath);
        await retriever.AddDocumentsAsync(docs, batchSize: 128, maxParallel: 4);
        _logger.LogInformation("Loaded {Count} TOGAF documents from {Path}", docs.Count, DocsPath);
    }
}
