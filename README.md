# My RAG Proof‑of‑Concept

A minimal **Retrieval‑Augmented Generation (RAG)** proof‑of‑concept built with .NET 8.0. The server demonstrates how to:

- Load documents from a local folder (`rag_index/kb.json`).
- Generate embeddings using **OpenRouter** (OpenAI `text-embedding-3-small`).
- Store vectors in a simple file‑based vector store.
- Expose an RPC service that can be called by a client to retrieve relevant chunks and generate answers.

## Repository Structure

```
My-RAG-PoC/
├─ RAGSharp.McpServer/          # The ASP.NET Core host
│   ├─ Program.cs               # Entry point – builds and runs the host
│   ├─ OpenRouterService.cs     # Wrapper around the OpenRouter completion API
│   ├─ RagRetrieverFactory.cs   # Creates a RagRetriever with an OpenAI embedding client
│   ├─ DocumentLoader.cs        # Loads documents from the index folder
│   ├─ RpcHandler.cs            # RPC endpoint implementation
│   ├─ rag_index/kb.json        # Sample knowledge‑base used for retrieval
│   └─ .env                     # Environment variable overrides (optional)
├─ .gitignore
├─ CLAUDE.md                    # Instructions for Claude Code
└─ README.md                    # **You are reading it!**
```

## Prerequisites

- **.NET SDK 8.0** (or later) – https://dotnet.microsoft.com/download
- **OpenRouter API key** – sign‑up at https://openrouter.ai and create an API key.
- **Optional:** `dotnet‑env` is used to load a `.env` file. Install it via NuGet (already referenced in the project).

## Getting Started

1. **Clone the repository**
   ```bash
   git clone <repo‑url>
   cd My-RAG-PoC
   ```

2. **Create a `.env` file** (or set environment variables directly) with your OpenRouter API key:
   ```dotenv
   OPENROUTER_API_KEY=sk-XXXXXXXXXXXXXXXXXXXX
   ```
   The project already includes `DotNetEnv` to load this file automatically.

3. **Restore NuGet packages and build**
   ```bash
   dotnet restore
   dotnet build
   ```

4. **Run the server**
   ```bash
   dotnet run --project RAGSharp.McpServer
   ```
   The host starts a background service (`McpServerHostedService`) and listens on the default Kestrel ports (http://localhost:5000 by default).

## How It Works

- **Embedding generation** – `RagRetrieverFactory` creates an `OpenAIEmbeddingClient` that talks to OpenRouter's embedding endpoint.
- **Vector store** – `FileVectorStore` persists vectors under `./rag_index`. The sample knowledge base (`kb.json`) is loaded by `DocumentLoader`.
- **Retrieval** – `RagRetriever` computes similarity between a query embedding and stored vectors, returning the top‑k most relevant chunks.
- **Answer generation** – `OpenRouterService` forwards the retrieved context to the OpenRouter completion API to generate a final answer.

## Configuration

| Variable | Description | Default |
|----------|-------------|---------|
| `OPENROUTER_API_KEY` | Your OpenRouter secret key. **Required**. | – |
| `DOTNET_ENV` | Enables loading of a `.env` file in the project root. | `true` |
| `RAG_INDEX_PATH` | Path to the vector store directory (relative to the executable). | `./rag_index` |
| `HOST_URL` | Base URL for the RPC service (used by clients). | `http://localhost:5000` |

You can override any of these by adding entries to a `.env` file or by setting system environment variables.

## Extending the PoC

- **Swap the embedding provider** – replace `OpenAIEmbeddingClient` with another provider that implements the same interface.
- **Persist to a different store** – implement `IVectorStore` (e.g., a SQLite or Redis backend) and update `RagRetrieverFactory`.
- **Add authentication** – plug in ASP.NET Core authentication middleware before registering `RpcHandler`.

## License

This proof‑of‑concept is provided under the **MIT License**. See the `LICENSE` file for details.

---
*Generated on 2026‑06‑14 by Claude Code.*