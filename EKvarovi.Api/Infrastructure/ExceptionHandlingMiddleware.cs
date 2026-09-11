using EKvarovi.Shared.Common;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace EKvarovi.Api.Infrastructure;

public sealed class ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (AppException ex)
        {
            await WriteAsync(context, ex.StatusCode, ex.Message, ex.Errors, ex.Payload);
        }
        catch (DbUpdateException ex) when (IsUniqueViolation(ex))
        {
            logger.LogWarning(ex, "Prekršeno jedinstveno ograničenje");
            await WriteAsync(context, 409, "Zapis s tim vrijednostima već postoji.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Neočekivana greška");
            await WriteAsync(context, 500, "Došlo je do neočekivane greške.");
        }
    }

    private static bool IsUniqueViolation(DbUpdateException ex)
        => ex.InnerException is PostgresException { SqlState: "23505" };

    private static Task WriteAsync(HttpContext context, int status, string message,
        IDictionary<string, string[]>? errors = null, object? payload = null)
    {
        context.Response.StatusCode = status;
        context.Response.ContentType = "application/json";
        return context.Response.WriteAsJsonAsync(new ApiErrorDto
        {
            Message = message,
            Errors = errors,
            Payload = payload
        });
    }
}
