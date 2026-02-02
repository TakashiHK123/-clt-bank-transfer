namespace BankTransfer.Infrastructure.DTOs;

public class IdempotencyRecordDto
{
    public string AccountId { get; set; } = string.Empty;
    public string TransferId { get; set; } = string.Empty;
    public string Key { get; set; } = string.Empty;
    public string RequestHash { get; set; } = string.Empty;
    public string ResponseJson { get; set; } = string.Empty;
    public string CreatedAt { get; set; } = string.Empty;
}
