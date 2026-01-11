using BankTransfer.Api.Auth;
using BankTransfer.Application.Abstractions;
using BankTransfer.Domain.Entities;
using BankTransfer.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace BankTransfer.Api.Bootstrap;

public static class DbInitializer
{
    public static async Task SeedAsync(IServiceProvider services)
    {
        var db = services.GetRequiredService<BankTransferDbContext>();
        var hasher = services.GetRequiredService<IPasswordHasher>();

        // Seed solo si no hay datos
        if (await db.Users.AsNoTracking().AnyAsync())
            return;

        // IDs fijos de USUARIOS
        var luanaUserId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var joseUserId  = Guid.Parse("22222222-2222-2222-2222-222222222222");
        var takaUserId  = Guid.Parse("33333333-3333-3333-3333-333333333333");

        // Crear usuarios (Id = userId)
        db.Users.AddRange(
            new User("luana", hasher.Hash("luana123"), luanaUserId),
            new User("jose", hasher.Hash("jose123"), joseUserId),
            new User("takashi", hasher.Hash("takashi123"), takaUserId)
        );

        // IDs fijos de CUENTAS (si querés que sean estables)
        var luanaPygAccountId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        var luanaUsdAccountId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");

        var josePygAccountId  = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");

        var takaPygAccountId  = Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd");
        var takaUsdAccountId  = Guid.Parse("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee");

        // Cuentas: para "varias cuentas por usuario" repetís el mismo userId
        db.Accounts.AddRange(
            Account.Seed(luanaPygAccountId, luanaUserId, "Luana", 1000m, "PYG"),
            Account.Seed(luanaUsdAccountId, luanaUserId, "Luana", 200m, "USD"),

            Account.Seed(josePygAccountId, joseUserId, "Jose", 500m, "PYG"),

            Account.Seed(takaPygAccountId, takaUserId, "Takashi", 250m, "PYG"),
            Account.Seed(takaUsdAccountId, takaUserId, "Takashi", 50m, "USD")
        );

        await db.SaveChangesAsync();
    }
}
