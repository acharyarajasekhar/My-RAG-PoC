using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using OpenRouter.NET.Models;
using RAGSharp.RAG;

public sealed class RpcHandler
{
    private readonly OpenRouterService _router;
    private readonly ILogger<RpcHandler> _logger;

    public RpcHandler(OpenRouterService router, ILogger<RpcHandler> logger)
    {
        _router = router;
        _logger = logger;
    }

    public async Task<McpResponse> HandleAsync(McpRequest request, RagRetriever retriever)
    {
        return request.Method switch
        {
            "rag/search" => await HandleSearchAsync(request, retriever),
            "rag/ask"    => await HandleAskAsync(request, retriever),
            "tools/list" => HandleToolsList(),
            _            => new McpResponse { Id = request.Id, Error = "Unknown method" }
        };
    }

    private static async Task<McpResponse> HandleSearchAsync(McpRequest request, RagRetriever retriever)
    {
        if (request.Params is null)
            return new McpResponse { Id = request.Id, Error = "Missing parameters for search" };

        var args = JsonSerializer.Deserialize<SearchArgs>(request.Params.ToString());
        if (args is null)
            return new McpResponse { Id = request.Id, Error = "Invalid search arguments" };

        var results = await retriever.Search(args.Query, topK: args.TopK ?? 5);
        return new McpResponse
        {
            Id = request.Id,
            Result = new { results = results.Select(r => new { r.Content, r.Score, r.Metadata }) }
        };
    }

    private async Task<McpResponse> HandleAskAsync(McpRequest request, RagRetriever retriever)
    {
        if (request.Params is null)
            return new McpResponse { Id = request.Id, Error = "Missing parameters for ask" };

        var args = JsonSerializer.Deserialize<AskArgs>(request.Params.ToString());
        if (args is null)
            return new McpResponse { Id = request.Id, Error = "Invalid ask arguments" };

        var results = await retriever.Search(args.Query, topK: 5);
        var context = string.Join("\n\n", results.Select(r => r.Content));
        var answer = await CallOpenRouterLlmAsync(args.Query, context);

        return new McpResponse
        {
            Id = request.Id,
            Result = new { answer, sources = results.Select(r => r.Metadata) }
        };
    }

    private async Task<string> CallOpenRouterLlmAsync(string query, string context)
    {
        var client = _router.CreateClient();
        var request = new ChatCompletionRequest
        {
            Model = "meta-llama/llama-3.2-3b-instruct:free",
            Messages = new List<Message>
            {
                Message.FromSystem(
                    $"You are a helpful assistant. Use the following context to answer the user's question accurately. " +
                    $"If the answer isn't in the context, say so.\n\nContext:\n{context}"),
                Message.FromUser(query)
            }
        };

        var response = await client.CreateChatCompletionAsync(request);
        return response?.Choices?[0]?.Message?.Content?.ToString() ?? "No response";
    }

    private static McpResponse HandleToolsList()
    {
        var tools = new[]
        {
            new { name = "rag/search", description = "Search RAG index for relevant chunks" },
            new { name = "rag/ask",    description = "Ask a question using RAG‑augmented generation" }
        };
        return new McpResponse { Id = null, Result = new { tools } };
    }
}
