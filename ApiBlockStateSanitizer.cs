using MuranoApp.Options;

namespace MuranoApp
{
    public static class ApiBlockStateSanitizer
    {
        public static List<string> NormalizeIgnoredPaths(IEnumerable<string>? input)
        {
            var paths = (input ?? Array.Empty<string>())
                .Where(p => !string.IsNullOrWhiteSpace(p))
                .Select(p => NormalizePath(p!))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            foreach (var required in ApiBlockDefaults.RequiredIgnoredPaths)
            {
                if (!paths.Contains(required, StringComparer.OrdinalIgnoreCase))
                {
                    paths.Add(required);
                }
            }

            return paths;
        }

        public static ApiBlockState NormalizeState(ApiBlockState state)
        {
            state.IgnoredPaths = NormalizeIgnoredPaths(state.IgnoredPaths);

            if (state.Mode != ApiBlockMode.Schedule)
            {
                state.StartUtc = null;
                state.EndUtc = null;
            }

            if (string.IsNullOrWhiteSpace(state.Message))
            {
                state.Message = ApiBlockDefaults.DefaultMessage;
            }

            if (state.StatusCode <= 0)
            {
                state.StatusCode = ApiBlockDefaults.DefaultStatusCode;
            }

            return state;
        }

        private static string NormalizePath(string path)
        {
            path = path.Trim();

            if (!path.StartsWith('/'))
            {
                path = "/" + path;
            }

            path = path.TrimEnd('/');

            return string.IsNullOrWhiteSpace(path) ? "/" : path;
        }
    }
}
