using System.Text.Json;
using RAGSharp.Embeddings.Providers;
using RAGSharp.IO;
using RAGSharp.RAG;
using RAGSharp.Stores;
using OpenRouter.NET;
using OpenRouter.NET.Models;

DotNetEnv.Env.Load();

await IsOpenRouterReachable();

var retriever = await InitializeRagRetriever();
await LoadTogafDocuments(retriever);

Console.WriteLine("RAG is ready with data...");

// MCP Server loop - reads JSON-RPC from stdin
while (true)
{
    var line = await Console.In.ReadLineAsync();
    if (string.IsNullOrEmpty(line)) continue;

    try
    {
        var request = JsonSerializer.Deserialize<McpRequest>(line);
        //var response = await HandleRequest(request, retriever);
        Console.WriteLine(JsonSerializer.Serialize(request));
    }
    catch (Exception ex)
    {
        Console.WriteLine(JsonSerializer.Serialize(new { error = ex.Message }));
    }
}

static async Task<bool> IsOpenRouterReachable()
{
    var apiKey = Environment.GetEnvironmentVariable("OPENROUTER_API_KEY") ?? string.Empty;

    using var client = new HttpClient();
    client.Timeout = TimeSpan.FromSeconds(10);
    client.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");
    
    try
    {
        // OpenRouter's auth verification endpoint
        var response = await client.GetAsync("https://openrouter.ai/api/v1/auth/key");
        
        if (response.IsSuccessStatusCode)
        {
            Console.WriteLine("✅ OpenRouter API is reachable and API key is valid");
            return true;
        }
        else if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
        {
            Console.WriteLine("❌ API key is invalid or expired");
        }
        else
        {
            Console.WriteLine($"⚠️ API responded with: {response.StatusCode}");
        }
    }
    catch (TaskCanceledException)
    {
        Console.WriteLine("❌ Connection timeout - OpenRouter is not reachable");
    }
    catch (HttpRequestException ex)
    {
        Console.WriteLine($"❌ Network error: {ex.Message}");
    }
    
    return false;
}

static async Task<RagRetriever> InitializeRagRetriever()
{
    var embeddingClient = new OpenAIEmbeddingClient(
        baseUrl: "https://openrouter.ai/api/v1",
        apiKey: Environment.GetEnvironmentVariable("OPENROUTER_API_KEY") ?? string.Empty,
        defaultModel: "openai/text-embedding-3-small"
    );

    return new RagRetriever(
        embeddings: embeddingClient,
        store: new FileVectorStore("./rag_index")  // Persistent storage
    );
}

static async Task LoadTogafDocuments(RagRetriever retriever)
{
    var docs = await new DirectoryLoader().LoadAsync("./Data/TogafDocs");
    await retriever.AddDocumentsAsync(docs, batchSize: 128, maxParallel: 4);
    Console.Error.WriteLine($"Loaded {docs.Count} TOGAF documents");
}

static async Task<McpResponse> HandleRequest(McpRequest request, RagRetriever retriever)
{
    return request.Method switch
    {
        "rag/search" => await HandleSearch(request, retriever),
        "rag/ask"    => await HandleAsk(request, retriever),
        "tools/list" => HandleToolsList(),
        _ => new McpResponse { Id = request.Id, Error = "Unknown method" }
    };
}

static async Task<McpResponse> HandleSearch(McpRequest request, RagRetriever retriever)
{
    var args = JsonSerializer.Deserialize<SearchArgs>(request.Params.ToString());
    var results = await retriever.Search(args.Query, topK: args.TopK ?? 5);
    
    return new McpResponse
    {
        Id = request.Id,
        Result = new { results = results.Select(r => new { r.Content, r.Score, r.Metadata }) }
    };
}

static async Task<McpResponse> HandleAsk(McpRequest request, RagRetriever retriever)
{
    var args = JsonSerializer.Deserialize<AskArgs>(request.Params.ToString());
    
    // Retrieve relevant chunks
    var results = await retriever.Search(args.Query, topK: 5);
    var context = string.Join("\n\n", results.Select(r => r.Content));
    
    // Call OpenRouter LLM
    var answer = await CallOpenRouterLlm(args.Query, context);
    
    return new McpResponse
    {
        Id = request.Id,
        Result = new { answer, sources = results.Select(r => r.Metadata) }
    };
}

static async Task<string> CallOpenRouterLlm(string query, string context)
{
    var client = new OpenRouterClient(apiKey: Environment.GetEnvironmentVariable("OPENROUTER_API_KEY") ?? string.Empty);

    var request = new ChatCompletionRequest
    {
        Model = "meta-llama/llama-3.2-3b-instruct:free",
        Messages = new List<Message>
        {
            Message.FromSystem($"You are a helpful assistant. Use the following context to answer the user's question accurately. If the answer isn't in the context, say so.\n\nContext:\n{context}"),
            Message.FromUser(query)
        }
    };

    var response = await client.CreateChatCompletionAsync(request);
    var answer = response?.Choices?[0]?.Message?.Content?.ToString() ?? "No response";

    return answer;
}

static McpResponse HandleToolsList()
{
    var tools = new[]
    {
        new { name = "rag/search", description = "Search RAG index for relevant chunks" },
        new { name = "rag/ask", description = "Ask a question using RAG-augmented generation" }
    };

    return new McpResponse
    {
        Id = null,
        Result = new { tools }
    };
}

// Helper models
record McpRequest(string JsonRpc = "2.0", object? Id = null, string? Method = null, object? Params = null);
record McpResponse(string JsonRpc = "2.0", object? Id = null, object? Result = null, string? Error = null);
record SearchArgs(string Query, int? TopK);
record AskArgs(string Query);