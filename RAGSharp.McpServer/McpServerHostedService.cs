using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RAGSharp.RAG;

public sealed class McpServerHostedService : BackgroundService
{
    private readonly ILogger<McpServerHostedService> _logger;
    private readonly OpenRouterService _router;
    private readonly RagRetrieverFactory _retrieverFactory;
    private readonly DocumentLoader _docLoader;
    private readonly RpcHandler _rpcHandler;
    private RagRetriever? _retriever;

    public McpServerHostedService(
        ILogger<McpServerHostedService> logger,
        OpenRouterService router,
        RagRetrieverFactory retrieverFactory,
        DocumentLoader docLoader,
        RpcHandler rpcHandler)
    {
        _logger = logger;
        _router = router;
        _retrieverFactory = retrieverFactory;
        _docLoader = docLoader;
        _rpcHandler = rpcHandler;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Check OpenRouter connectivity (non‑fatal)
        if (!await _router.IsReachableAsync())
        {
            _logger.LogWarning("OpenRouter is not reachable – the server will start but LLM calls will fail.");
        }

        // Initialise retriever and load documents
        _retriever = await _retrieverFactory.CreateAsync();
        await _docLoader.LoadAsync(_retriever);

        _logger.LogInformation("RAG is ready with data. Listening for JSON‑RPC requests…");

        while (!stoppingToken.IsCancellationRequested)
        {
            var line = await Console.In.ReadLineAsync();
            if (string.IsNullOrWhiteSpace(line))
                continue;

            try
            {
                var request = JsonSerializer.Deserialize<McpRequest>(line);
                if (request is null)
                {
                    await WriteErrorAsync("Invalid request format");
                    continue;
                }

                var response = await _rpcHandler.HandleAsync(request, _retriever);
                Console.WriteLine(JsonSerializer.Serialize(response));
            }
            catch (Exception ex)
            {
                await WriteErrorAsync(ex.Message);
            }
        }
    }

    private static async Task WriteErrorAsync(string message)
    {
        var errorObj = new { error = message };
        Console.WriteLine(JsonSerializer.Serialize(errorObj));
        await Task.CompletedTask;
    }
}
