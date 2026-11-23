using BocciaServer.Services;
using Microsoft.AspNetCore.Diagnostics;

namespace BocciaServer.Infrastructure;

/// <summary>Convierte las excepciones de dominio en respuestas ProblemDetails (404 / 409).</summary>
public class ProblemDetailsExceptionHandler(IProblemDetailsService problemDetails) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext context, Exception exception, CancellationToken ct)
    {
        var (status, title) = exception switch
        {
            NotFoundException => (StatusCodes.Status404NotFound, "No encontrado"),
            GameRuleException => (StatusCodes.Status409Conflict, "Acción no permitida"),
            _ => (0, ""),
        };
        if (status == 0) return false;

        context.Response.StatusCode = status;
        return await problemDetails.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = context,
            Exception = exception,
            ProblemDetails = { Status = status, Title = title, Detail = exception.Message },
        });
    }
}
