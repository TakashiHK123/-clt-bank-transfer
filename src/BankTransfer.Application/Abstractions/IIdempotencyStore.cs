namespace BankTransfer.Application.Abstractions;

public interface IIdempotencyStore
{
    Task<IdempotencyResult?> GetAsync(string key, CancellationToken ct);
    Task SaveSuccessAsync(string key, string requestHash, string responseJson, CancellationToken ct);
}

public sealed record IdempotencyResult(string RequestHash, string ResponseJson);