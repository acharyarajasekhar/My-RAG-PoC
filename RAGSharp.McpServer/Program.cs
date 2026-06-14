using DotNetEnv;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

// Load environment variables from .env (if present)
Env.Load();

var host = Host.CreateDefaultBuilder()
    .ConfigureLogging(logging =>
    {
        logging.ClearProviders();
        logging.AddSimpleConsole(options =>
        {
            options.SingleLine = true;
            options.TimestampFormat = "HH:mm:ss ";
        });
    })
    .ConfigureServices((context, services) =>
    {
        services.AddSingleton<OpenRouterService>();
        services.AddSingleton<RagRetrieverFactory>();
        services.AddSingleton<DocumentLoader>();
        services.AddSingleton<RpcHandler>();
        services.AddHostedService<McpServerHostedService>();
    })
    .Build();

await host.RunAsync();
