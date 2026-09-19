using System.Net;
using FluentValidation;
using Microsoft.AspNetCore.Http;
using OrderProcessing.Application.Common.Constants;
using OrderProcessing.Application.Common.Models;

namespace OrderProcessing.Api.Middleware;

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
        catch (ValidationException ex)
        {
            _logger.LogWarning("Validation failed: {Errors}", string.Join("; ", ex.Errors.Select(e => e.ErrorMessage)));
            await WriteErrorAsync(context, HttpStatusCode.BadRequest,
                new ApiError(ErrorCodes.ValidationError, string.Join(" ", ex.Errors.Select(e => e.ErrorMessage))));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception processing {Method} {Path}", context.Request.Method, context.Request.Path);
            await WriteErrorAsync(context, HttpStatusCode.InternalServerError,
                new ApiError("internal_error", "An unexpected error occurred."));
        }
    }

    private static Task WriteErrorAsync(HttpContext context, HttpStatusCode statusCode, ApiError error)
    {
        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int)statusCode;
        return context.Response.WriteAsJsonAsync(error);
    }
}
