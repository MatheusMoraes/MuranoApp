using Microsoft.Extensions.Options;

namespace MuranoApp.Options
{
    public sealed class ApiBlockMiddleware
    {
        private readonly RequestDelegate _next;

        public ApiBlockMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context, IApiBlockStateStore store)
        {
            var state = store.Get();

            if (ShouldIgnore(context.Request.Path, state.IgnoredPaths))
            {
                await _next(context);
                return;
            }

            if (!IsBlocked(state))
            {
                await _next(context);
                return;
            }

            context.Response.StatusCode = state.StatusCode;
            context.Response.ContentType = "application/json";

            await context.Response.WriteAsJsonAsync(new
            {
                message = state.Message,
                mode = state.Mode.ToString(),
                updatedAtUtc = state.UpdatedAtUtc,
                start = state.StartUtc.HasValue
                    ? BrazilTime.ConvertUtcToBrazilLocal(state.StartUtc.Value).ToString("yyyy-MM-ddTHH:mm")
                    : null,
                end = state.EndUtc.HasValue
                    ? BrazilTime.ConvertUtcToBrazilLocal(state.EndUtc.Value).ToString("yyyy-MM-ddTHH:mm")
                    : null
            });
        }

        private static bool ShouldIgnore(PathString path, List<string> ignoredPaths)
        {
            foreach (var ignoredPath in ignoredPaths)
            {
                if (path.StartsWithSegments(ignoredPath, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool IsBlocked(ApiBlockState state)
        {
            return state.Mode switch
            {
                ApiBlockMode.Disabled => false,
                ApiBlockMode.Full => true,
                ApiBlockMode.Schedule => IsInsideWindow(state),
                _ => false
            };
        }

        private static bool IsInsideWindow(ApiBlockState state)
        {
            if (state.StartUtc is null || state.EndUtc is null)
            {
                return false;
            }

            var nowUtc = DateTimeOffset.UtcNow;
            return nowUtc >= state.StartUtc.Value && nowUtc < state.EndUtc.Value;
        }
    }
}
