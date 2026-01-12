using System.Text;
using BankTransfer.Api.Auth;
using BankTransfer.Api.Bootstrap;
using BankTransfer.Api.Middleware;
using BankTransfer.Application.Abstractions;
using BankTransfer.Application.Abstractions.Repositories;
using BankTransfer.Application.Abstractions.Services;
using BankTransfer.Application.Services;
using BankTransfer.Infrastructure.Auth;
using BankTransfer.Infrastructure.Persistence;
using BankTransfer.Infrastructure.Repositories;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddScoped<ITransferService, TransferService>();

// =========================
// DI - Repositories/Services
// =========================
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddSingleton<IPasswordHasher, PasswordHasherService>();

// Application Services
builder.Services.AddScoped<IAccountService, AccountService>();
builder.Services.AddScoped<IAuthService, AuthService>(); 

// =========================
// Swagger
// =========================
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "BankTransfer API", Version = "v1" });

    var jwtScheme = new OpenApiSecurityScheme
    {
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        Description = "Put ONLY the JWT token here",
        Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
    };

    c.AddSecurityDefinition("Bearer", jwtScheme);
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        { jwtScheme, new List<string>() }
    });
});

// =========================
// JWT
// =========================
builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection("Jwt"));

builder.Services.AddSingleton<TokenService>();
builder.Services.AddSingleton<ITokenService>(sp => sp.GetRequiredService<TokenService>());

var jwt = builder.Configuration.GetSection("Jwt").Get<JwtOptions>()!;
builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(o =>
    {
        o.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwt.Issuer,
            ValidAudience = jwt.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.Key))
        };
    });

builder.Services.AddAuthorization();

// =========================
// EF Core SQLite
// =========================
builder.Services.AddDbContext<BankTransferDbContext>(opt =>
{
    var cs = builder.Configuration.GetConnectionString("Default")!;
    opt.UseSqlite(cs);
});

// =========================
// Repositories + UoW + UseCases
// =========================
builder.Services.AddScoped<IAccountRepository, AccountRepository>();
builder.Services.AddScoped<ITransferRepository, TransferRepository>();
builder.Services.AddScoped<IIdempotencyStore, IdempotencyStore>();
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
builder.Services.AddScoped<TransferFundsService>();

// =========================
// Middleware
// =========================
builder.Services.AddTransient<ExceptionHandlingMiddleware>();

var app = builder.Build();

app.UseMiddleware<ExceptionHandlingMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// =========================
// Migraciones + Seed al iniciar (único lugar)
// =========================
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;

    var db = services.GetRequiredService<BankTransferDbContext>();
    await db.Database.MigrateAsync();

    await DbInitializer.SeedAsync(services);
}

app.Run();
