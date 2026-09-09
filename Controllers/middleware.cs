using System.Diagnostics;
using System.Text.Json;

namespace MuranoApp.Controllers
{

    // Catches any unhandled exception so the client always gets a real JSON
    // 500 response with CORS headers attached, instead of the connection
    // resetting mid-pipeline (which strips headers, including
    // Access-Control-Allow-Origin, and makes the browser report a
    // misleading "CORS blocked" error that hides the actual 500).
    public class ExceptionHandlingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ExceptionHandlingMiddleware> _logger;

        public ExceptionHandlingMiddleware(
            RequestDelegate next,
            ILogger<ExceptionHandlingMiddleware> logger)
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
                _logger.LogError(ex, "Unhandled exception processing {Method} {Path}", context.Request.Method, context.Request.Path);

                if (context.Response.HasStarted)
                {
                    throw;
                }

                context.Response.Clear();
                context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                context.Response.ContentType = "application/json";

                var origin = context.Request.Headers.Origin.ToString();
                context.Response.Headers["Access-Control-Allow-Origin"] = string.IsNullOrEmpty(origin) ? "*" : origin;
                context.Response.Headers["Vary"] = "Origin";

                var payload = JsonSerializer.Serialize(new
                {
                    error = "Ocorreu um erro interno ao processar a requisição."
                });

                await context.Response.WriteAsync(payload);
            }
        }
    }

    public class RequestTimingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<RequestTimingMiddleware> _logger;

        public RequestTimingMiddleware(
            RequestDelegate next,
            ILogger<RequestTimingMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var stopwatch = Stopwatch.StartNew();

            try
            {
                await _next(context);
            }
            finally
            {
                stopwatch.Stop();

                _logger.LogInformation(
                    "HTTP {Method} {Path} | Status: {StatusCode} | {Duration} ms",
                    context.Request.Method,
                    context.Request.Path,
                    context.Response.StatusCode,
                    stopwatch.ElapsedMilliseconds
                );
            }
        }

    }
}
