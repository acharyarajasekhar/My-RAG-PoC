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
            baseUrl: Environment.GetEnvironmentVariable("EMBEDDING_MODEL_BASE_URL") ?? string.Empty,
            apiKey: Environment.GetEnvironmentVariable("EMBEDDING_MODEL_API_KEY") ?? string.Empty,
            defaultModel: Environment.GetEnvironmentVariable("EMBEDDING_MODEL") ?? string.Empty
        );

        var retriever = new RagRetriever(
            embeddings: embeddingClient,
            store: new FileVectorStore("./rag_index"));

        return Task.FromResult(retriever);
    }
}
