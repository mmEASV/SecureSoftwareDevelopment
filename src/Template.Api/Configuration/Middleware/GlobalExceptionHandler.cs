using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Template.Api.Domain.Exceptions;

namespace Template.Api.Configuration.Middleware;

public class GlobalExceptionHandler : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> _logger;

    public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger)
    {
        _logger = logger;
    }

    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        // Default values
        int statusCode = StatusCodes.Status500InternalServerError;
        string title = "Server error";
        string detail = exception.Message;

        if (exception is CustomException customException)
        {
            _logger.LogError(exception, "Custom exception occurred: {Message}", exception.Message);

            switch (exception)
            {
                case NotFoundException notFoundException:
                    statusCode = StatusCodes.Status404NotFound;
                    title = "NotFound";
                    detail = notFoundException.Message;
                    break;
                default:
                    statusCode = StatusCodes.Status500InternalServerError;
                    title = "CustomExceptionNotImplemented";
                    detail = customException.Message;
                    break;
            }
        }
        else
        {
            _logger.LogError(exception, "Unhandled exception occurred: {Message}", exception.Message);
        }

        var problemDetails = new ProblemDetails
        {
            Status = statusCode,
            Title = title,
            Detail = detail
        };
        httpContext.Response.StatusCode = problemDetails.Status.Value;

        await httpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken);

        return true;
    }
}
