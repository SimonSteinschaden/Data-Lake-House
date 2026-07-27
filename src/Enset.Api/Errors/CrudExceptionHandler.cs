using Enset.Application.Crud;
using Enset.Application.Curation;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Enset.Api.Errors;

public sealed class CrudExceptionHandler(IProblemDetailsService problemDetails) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext context, Exception exception,
        CancellationToken cancellationToken)
    {
        ProblemDetails details = exception switch
        {
            CrudValidationException validation => new ValidationProblemDetails(
                validation.Errors.ToDictionary(x => x.Key, x => x.Value))
                { Title = "Validierung fehlgeschlagen", Status = StatusCodes.Status400BadRequest },
            CrudNotFoundException => new()
                { Title = "Nicht gefunden", Detail = exception.Message, Status = StatusCodes.Status404NotFound },
            CrudConflictException => new()
                { Title = "Konflikt", Detail = exception.Message, Status = StatusCodes.Status409Conflict },
            CurationValidationException => new()
                { Title = "Validierung fehlgeschlagen", Detail = exception.Message, Status = StatusCodes.Status400BadRequest },
            CurationNotFoundException => new()
                { Title = "Nicht gefunden", Detail = exception.Message, Status = StatusCodes.Status404NotFound },
            CurationConflictException => new()
                { Title = "Konflikt", Detail = exception.Message, Status = StatusCodes.Status409Conflict },
            DbUpdateConcurrencyException => new()
                { Title = "Konflikt", Detail = "Die Kurationsaufgabe wurde zwischenzeitlich geändert.",
                  Status = StatusCodes.Status409Conflict },
            _ => new()
                { Title = "Interner Serverfehler", Detail = "Die Anfrage konnte nicht verarbeitet werden.",
                  Status = StatusCodes.Status500InternalServerError }
        };
        context.Response.StatusCode = details.Status!.Value;
        return await problemDetails.TryWriteAsync(new ProblemDetailsContext
            { HttpContext = context, ProblemDetails = details });
    }
}
