using BankTransfer.Domain.Exceptions;
using Microsoft.EntityFrameworkCore;
using System.Net;
using System.Text.Json;

namespace BankTransfer.Api.Middleware;

public sealed class ExceptionHandlingMiddleware : IMiddleware
{
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;
    public ExceptionHandlingMiddleware(ILogger<ExceptionHandlingMiddleware> logger) => _logger = logger;

    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        try
        {
            await next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled error");

            var (status, title) = ex switch
            {
                AccountNotFoundException => (HttpStatusCode.NotFound, "Account not found"),
                InsufficientFundsException => (HttpStatusCode.UnprocessableEntity, "Insufficient funds"),
                SameAccountTransferException => (HttpStatusCode.BadRequest, "Invalid transfer"),
                IdempotencyConflictException => (HttpStatusCode.Conflict, "Idempotency conflict"),
                DbUpdateConcurrencyException => (HttpStatusCode.Conflict, "Concurrency conflict"),
                _ => (HttpStatusCode.InternalServerError, "Internal server error")
            };

            context.Response.StatusCode = (int)status;
            context.Response.ContentType = "application/json";

            var problem = new
            {
                title,
                status = (int)status,
                detail = ex.Message,
                traceId = context.TraceIdentifier
            };

            await context.Response.WriteAsync(JsonSerializer.Serialize(problem));
        }
    }
}