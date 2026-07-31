namespace MuranoApp.Options
{
    public sealed class ApiBlockState
    {
        public ApiBlockMode Mode { get; set; } = ApiBlockMode.Disabled;
        public DateTimeOffset? StartUtc { get; set; }
        public DateTimeOffset? EndUtc { get; set; }
        public int StatusCode { get; set; } = ApiBlockDefaults.DefaultStatusCode;
        public string Message { get; set; } = ApiBlockDefaults.DefaultMessage;
        public List<string> IgnoredPaths { get; set; } =
            ApiBlockStateSanitizer.NormalizeIgnoredPaths(ApiBlockDefaults.DefaultIgnoredPaths);
        public DateTimeOffset UpdatedAtUtc { get; set; } = DateTimeOffset.UtcNow;
        public string? UpdatedBy { get; set; }
    }

    public enum ApiBlockMode
    {
        Disabled = 0,
        Full = 1,
        Schedule = 2
    }

    public static class ApiBlockDefaults
    {
        public const string AdminPath = "/admin";

        public static readonly string[] RequiredIgnoredPaths =
        {
        "/admin"
    };

        public static readonly string[] DefaultIgnoredPaths =
        {
        "/admin",
        "/health",
        "/swagger"
    };

        public const string DefaultTimeZoneId = "E. South America Standard Time";
        public const int DefaultStatusCode = StatusCodes.Status503ServiceUnavailable;
        public const string DefaultMessage = "API temporariamente indisponível";
    }
}
