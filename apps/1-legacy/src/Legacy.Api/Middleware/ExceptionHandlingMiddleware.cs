using Legacy.Domain.Exceptions;
using Microsoft.AspNetCore.Mvc;

namespace Legacy.Api.Middleware;

public sealed class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (DomainException exception)
        {
            _logger.LogWarning(exception, "Domain exception {ErrorCode}", exception.ErrorCode);

            context.Response.StatusCode = exception.StatusCode;
            context.Response.ContentType = "application/problem+json";

            var problem = new ProblemDetails
            {
                Title = exception.ErrorCode,
                Detail = exception.Message,
                Status = exception.StatusCode
            };

            await context.Response.WriteAsJsonAsync(problem);
        }
    }
}
