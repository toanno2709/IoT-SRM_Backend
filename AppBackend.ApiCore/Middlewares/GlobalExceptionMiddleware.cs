using AppBackend.BusinessObjects.Exceptions;
using AppBackend.Services.ApiModels.Commons;
using System.Net;
using System.Text.Json;

namespace AppBackend.ApiCore.Middlewares
{
    /// <summary>
    /// Global exception handler middleware to catch and process all exceptions
    /// </summary>
    public class GlobalExceptionMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<GlobalExceptionMiddleware> _logger;

        public GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger)
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
            catch (Exception ex)
            {
                await HandleExceptionAsync(context, ex);
            }
        }

        private async Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            context.Response.ContentType = "application/json";
            
            ResultModel response;

            switch (exception)
            {
                case AppException appEx:
                    // Handle custom AppException with specific status codes
                    context.Response.StatusCode = appEx.StatusCode;
                    response = new ResultModel
                    {
                        IsSuccess = false,
                        ResponseCode = appEx.Code,
                        Message = appEx.Message,
                        StatusCode = appEx.StatusCode
                    };
                    
                    // Log warning for client errors (4xx)
                    if (appEx.StatusCode >= 400 && appEx.StatusCode < 500)
                    {
                        _logger.LogWarning(appEx, "Client error: {Code} - {Message}", appEx.Code, appEx.Message);
                    }
                    else
                    {
                        _logger.LogError(appEx, "Application error: {Code} - {Message}", appEx.Code, appEx.Message);
                    }
                    break;

                case BadRequestException badRequestEx:
                    // Handle BadRequestException
                    context.Response.StatusCode = badRequestEx.StatusCode;
                    response = new ResultModel
                    {
                        IsSuccess = false,
                        ResponseCode = badRequestEx.ErrorDetail.ErrorCode,
                        Message = badRequestEx.ErrorDetail.ErrorMessage?.ToString() ?? "Bad request",
                        StatusCode = badRequestEx.StatusCode
                    };
                    _logger.LogWarning(badRequestEx, "Bad request: {ErrorCode}", badRequestEx.ErrorDetail.ErrorCode);
                    break;

                case ErrorException errorEx:
                    // Handle generic ErrorException
                    context.Response.StatusCode = errorEx.StatusCode;
                    response = new ResultModel
                    {
                        IsSuccess = false,
                        ResponseCode = errorEx.ErrorDetail.ErrorCode,
                        Message = errorEx.ErrorDetail.ErrorMessage?.ToString() ?? "An error occurred",
                        StatusCode = errorEx.StatusCode
                    };
                    _logger.LogError(errorEx, "Error: {ErrorCode}", errorEx.ErrorDetail.ErrorCode);
                    break;

                default:
                    // Handle unexpected exceptions
                    context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                    response = new ResultModel
                    {
                        IsSuccess = false,
                        ResponseCode = "INTERNAL_SERVER_ERROR",
                        Message = "An unexpected error occurred. Please try again later.",
                        StatusCode = (int)HttpStatusCode.InternalServerError
                    };
                    
                    // Log full exception details for unexpected errors
                    _logger.LogError(exception, "Unhandled exception: {Message}", exception.Message);
                    break;
            }

            var jsonResponse = JsonSerializer.Serialize(response, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            await context.Response.WriteAsync(jsonResponse);
        }
    }

    /// <summary>
    /// Extension method to add the global exception middleware to the pipeline
    /// </summary>
    public static class GlobalExceptionMiddlewareExtensions
    {
        public static IApplicationBuilder UseGlobalExceptionHandler(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<GlobalExceptionMiddleware>();
        }
    }
}
