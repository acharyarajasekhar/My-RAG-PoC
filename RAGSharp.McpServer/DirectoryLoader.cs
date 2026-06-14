// Updated DirectoryLoader to avoid type conflict
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using RAGSharp.IO;


public sealed class DirectoryLoader
{
    private readonly ILogger<DirectoryLoader> _logger;

    public DirectoryLoader(ILogger<DirectoryLoader>? logger = null) =>
        _logger = logger ?? NullLogger<DirectoryLoader>.Instance;

    /// <summary>
    /// Loads all supported files from <paramref name="folderPath"/>.
    /// Returns a list of <see cref="Document"/> objects ready for the retriever.
    /// </summary>
    public async Task<IReadOnlyList<Document>> LoadAsync(string folderPath)
    {
        var docs = new List<Document>();

        if (!Directory.Exists(folderPath))
        {
            _logger.LogWarning("Docs folder not found: {Folder}", folderPath);
            return docs;
        }

        var files = Directory.EnumerateFiles(folderPath, "*.*", SearchOption.AllDirectories);
        foreach (var file in files)
        {
            var ext = Path.GetExtension(file).ToLowerInvariant();

            // Plain‑text based files – read directly
            if (ext == ".txt" || ext == ".md" || ext == ".json")
            {
                var content = await File.ReadAllTextAsync(file);
                if (!string.IsNullOrWhiteSpace(content))
                    docs.Add(MakeDocument(file, ext, content));
                continue;
            }

            // PDF files – extract with PdfPig
            if (ext == ".pdf")
            {
                var content = PdfTextExtractor.Extract(file);
                if (!string.IsNullOrWhiteSpace(content))
                    docs.Add(MakeDocument(file, ext, content));
                continue;
            }

            // Unsupported – skip but log for visibility
            _logger.LogDebug("Skipping unsupported file: {File}", file);
        }

        _logger.LogInformation("Loaded {Count} document(s) from {Folder}", docs.Count, folderPath);
        return docs;
    }

    private static Document MakeDocument(string filePath, string ext, string content) =>
        new Document(content, filePath, new Dictionary<string, string>
        {
            ["source"] = filePath,
            ["extension"] = ext
        });
}