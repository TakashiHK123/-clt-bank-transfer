using System.Reflection;
using BankTransfer.Domain.Entities;

namespace BankTransfer.Infrastructure.Extensions;

public static class TransferExtensions
{
    public static Transfer CreateFromDto(Guid id, Guid fromAccountId, Guid toAccountId, decimal amount, DateTimeOffset createdAt)
    {
        // Create transfer with dummy values first
        var transfer = new Transfer(fromAccountId, toAccountId, amount, "PYG", "temp-key");
        
        // Use reflection to set the Id and CreatedAt
        var idField = typeof(Transfer).GetField("<Id>k__BackingField", BindingFlags.NonPublic | BindingFlags.Instance);
        var createdAtField = typeof(Transfer).GetField("<CreatedAt>k__BackingField", BindingFlags.NonPublic | BindingFlags.Instance);
        
        idField?.SetValue(transfer, id);
        createdAtField?.SetValue(transfer, createdAt);
        
        return transfer;
    }
}
