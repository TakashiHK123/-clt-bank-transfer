using System.Data;
using BankTransfer.Application.Abstractions;

namespace BankTransfer.Infrastructure.Repositories;

public sealed class UnitOfWork : IUnitOfWork, IDisposable
{
    private readonly IDbConnection _connection;
    private IDbTransaction? _transaction;

    public UnitOfWork(IDbConnection connection) => _connection = connection;

    public async Task<int> SaveChangesAsync(CancellationToken ct = default)
    {
        if (_transaction is null)
            return 0;

        await Task.Run(() => _transaction.Commit(), ct);
        return 1;
    }

    public void BeginTransaction()
    {
        if (_connection.State != ConnectionState.Open)
            _connection.Open();
        
        _transaction = _connection.BeginTransaction();
    }

    public void Dispose()
    {
        _transaction?.Dispose();
        _connection?.Dispose();
    }
}