using AvinyaAICRM.Application.Interfaces.ServiceInterface;
using AvinyaAICRM.Domain.Entities.ErrorLogs;
using System.Diagnostics;
using System.Net;
using System.Text.Json;

namespace AvinyaAICRM.API.Filters
{
    public class GlobalExceptionMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<GlobalExceptionMiddleware> _logger;
        private readonly IServiceScopeFactory _scopeFactory;

        public GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger,IServiceScopeFactory scopeFactory)
        {
            _next = next;
            _logger = logger;
            _scopeFactory = scopeFactory;
        }

        public async Task Invoke(HttpContext context)
        {   
            try
            {
                _logger.LogInformation("Global exception filter wokrfed");
                await _next(context);
            }
            catch (Exception ex)
            {
                using var scope = _scopeFactory.CreateScope();
                var errorLogService = scope.ServiceProvider
                    .GetRequiredService<IErrorLogService>();
                var exceptionDetails = GetExceptionDetails(ex);

                _logger.LogError("Exception caught by global handler \n" + "Message : {Message}" + "Method : {Method}" + "File: {File}"
                    + "Line: {Line}"
                    + "Path: {Path}",
                    exceptionDetails.Message,
                    exceptionDetails.Method,
                    exceptionDetails.File,
                    exceptionDetails.Line,
                    context.Request.Path);

                await errorLogService.LogAsync(new ErrorLogs
                {
                    Message = exceptionDetails.Message,
                    Method = exceptionDetails.Method,
                    FileName = exceptionDetails.File,
                    LineNumber = exceptionDetails.Line,
                    Path = context.Request.Path,
                    StackTrace = ex.StackTrace
                });

                await HandleExceptionAsync(context, ex);
            }
        }

        private static Task HandleExceptionAsync(HttpContext context, Exception ex)
        {
            var response = new
            {
                success = false,
                message = "An unexpected error occurred.",
                error = ex.Message
            };

            context.Response.ContentType = "application/json";
            context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;

            return context.Response.WriteAsync(JsonSerializer.Serialize(response));
        }

        private static (string Message, string Method, string File, int Line) GetExceptionDetails(Exception ex)
        {
            var stackTrace = new StackTrace(ex, true);
            var frame = stackTrace.GetFrames()?.FirstOrDefault(f => f.GetFileLineNumber() > 0);

            string file = frame?.GetFileName() ?? string.Empty;
            string method = frame?.GetMethod()?.Name ?? string.Empty;
            int line = frame?.GetFileLineNumber() ?? 0;

            return (ex.Message, method, file, line);

        }
    }
}
