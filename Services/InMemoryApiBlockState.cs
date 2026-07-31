using MuranoApp.Options;

namespace MuranoApp.Services
{
    public sealed class InMemoryApiBlockStateStore : IApiBlockStateStore
    {
        private readonly object _sync = new();

        private ApiBlockState _state = ApiBlockStateSanitizer.NormalizeState(new ApiBlockState
        {
            Mode = ApiBlockMode.Disabled,
            Message = ApiBlockDefaults.DefaultMessage,
            StatusCode = ApiBlockDefaults.DefaultStatusCode,
            IgnoredPaths = ApiBlockDefaults.DefaultIgnoredPaths.ToList(),
            UpdatedAtUtc = DateTimeOffset.UtcNow
        });

        public ApiBlockState Get()
        {
            lock (_sync)
            {
                return Clone(_state);
            }
        }

        public void Set(ApiBlockState state)
        {
            lock (_sync)
            {
                state = ApiBlockStateSanitizer.NormalizeState(state);
                _state = Clone(state);
                _state.UpdatedAtUtc = DateTimeOffset.UtcNow;
            }
        }

        private static ApiBlockState Clone(ApiBlockState source)
        {
            return new ApiBlockState
            {
                Mode = source.Mode,
                StartUtc = source.StartUtc,
                EndUtc = source.EndUtc,
                StatusCode = source.StatusCode,
                Message = source.Message,
                IgnoredPaths = source.IgnoredPaths.ToList(),
                UpdatedAtUtc = source.UpdatedAtUtc,
                UpdatedBy = source.UpdatedBy
            };
        }
    }
}
