using System;
using System.Threading.Tasks;
using OpenRouter.NET;
using RAGSharp.Embeddings.Providers;
using RAGSharp.RAG;
using RAGSharp.Stores;

public sealed class RagRetrieverFactory
{
    public Task<RagRetriever> CreateAsync()
    {
        var embeddingClient = new OpenAIEmbeddingClient(
            baseUrl: "https://openrouter.ai/api/v1",
            apiKey: Environment.GetEnvironmentVariable("OPENROUTER_API_KEY") ?? string.Empty,
            defaultModel: "openai/text-embedding-3-small");

        var retriever = new RagRetriever(
            embeddings: embeddingClient,
            store: new FileVectorStore("./rag_index"));

        return Task.FromResult(retriever);
    }
}
