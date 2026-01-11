using BankTransfer.Application.Abstractions;
using BankTransfer.Domain.Entities;
using BankTransfer.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace BankTransfer.Api.Bootstrap;

public static class DbInitializer
{
    public static async Task SeedAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();

        var db = scope.ServiceProvider.GetRequiredService<BankTransferDbContext>();
        var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();

        await db.Database.MigrateAsync();

        if (await db.Users.AnyAsync())
            return;

        var luanaAcc = await db.Accounts.FirstOrDefaultAsync(a => a.Name == "Luana");
        var joseAcc   = await db.Accounts.FirstOrDefaultAsync(a => a.Name == "Jose");
        var takashiAcc = await db.Accounts.FirstOrDefaultAsync(a => a.Name == "Takashi");
        

        if (luanaAcc is null)
        {
            luanaAcc = new Account(name: "Luana",initialBalance:1000m);
            db.Accounts.Add(luanaAcc);
        }

        if (joseAcc is null)
        {
            joseAcc = new Account(name: "Jose", initialBalance: 500m);
            db.Accounts.Add(joseAcc);
        }

        if (takashiAcc is null)
        {
            takashiAcc = new Account(name: "Takashi", initialBalance: 250m);
            db.Accounts.Add(takashiAcc);
        }

        await db.SaveChangesAsync();

        db.Users.AddRange(
            new User(username: "luana", passwordHash: hasher.Hash("luana123"), accountId: luanaAcc.Id),
            new User(username: "jose",   passwordHash: hasher.Hash("jose123"),   accountId: joseAcc.Id),
            new User(username: "takashi",  passwordHash: hasher.Hash("takashi123"),accountId: takashiAcc.Id)
        );

        await db.SaveChangesAsync();
    }
}