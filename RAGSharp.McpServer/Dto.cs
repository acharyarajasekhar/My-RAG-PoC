public record McpRequest(string JsonRpc = "2.0", object? Id = null, string? Method = null, object? Params = null);
public record McpResponse(string JsonRpc = "2.0", object? Id = null, object? Result = null, string? Error = null);
public record SearchArgs(string Query, int? TopK);
public record AskArgs(string Query);
