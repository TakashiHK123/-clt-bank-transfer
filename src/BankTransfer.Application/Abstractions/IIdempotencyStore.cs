namespace BankTransfer.Application.Abstractions;

public interface IIdempotencyStore
{
    Task<IdempotencyResult?> GetAsync(Guid ownerId, string key, CancellationToken ct);
    Task SaveSuccessAsync(Guid ownerId, string key, string requestHash, string responseJson, CancellationToken ct);
}

public sealed record IdempotencyResult(string RequestHash, string ResponseJson);