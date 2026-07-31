using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MuranoApp.Models;
using MuranoApp.Options;

namespace MuranoApp.Controllers
{
    [ApiController]
    [Route("admin/api-block")]
    public sealed class ApiBlockAdminController : ControllerBase
    {
        private readonly IApiBlockStateStore _store;

        public ApiBlockAdminController(IApiBlockStateStore store)
        {
            _store = store;
        }

        [HttpGet]
        public ActionResult<ApiBlockResponse> Get()
        {
            var state = _store.Get();
            return Ok(ToResponse(state));
        }

        [HttpPut]
        public ActionResult<ApiBlockResponse> Put([FromBody] UpdateApiBlockRequest request)
        {
            var current = _store.Get();

            var newState = new ApiBlockState
            {
                Mode = request.Mode,
                StartUtc = request.Start.HasValue
                    ? BrazilTime.ConvertBrazilLocalToUtc(request.Start.Value)
                    : null,
                EndUtc = request.End.HasValue
                    ? BrazilTime.ConvertBrazilLocalToUtc(request.End.Value)
                    : null,
                StatusCode = request.StatusCode ?? ApiBlockDefaults.DefaultStatusCode,
                Message = string.IsNullOrWhiteSpace(request.Message)
                    ? ApiBlockDefaults.DefaultMessage
                    : request.Message,
                IgnoredPaths = request.IgnoredPaths is null
                    ? current.IgnoredPaths
                    : ApiBlockStateSanitizer.NormalizeIgnoredPaths(request.IgnoredPaths),
                UpdatedBy = "manual-admin-endpoint"
            };

            _store.Set(newState);

            return Ok(ToResponse(_store.Get()));
        }

        [HttpPost("disable")]
        public ActionResult<ApiBlockResponse> Disable()
        {
            var current = _store.Get();

            current.Mode = ApiBlockMode.Disabled;
            current.StartUtc = null;
            current.EndUtc = null;
            current.UpdatedBy = "manual-admin-endpoint";

            _store.Set(current);

            return Ok(ToResponse(_store.Get()));
        }

        private static ApiBlockResponse ToResponse(ApiBlockState state)
        {
            return new ApiBlockResponse
            {
                Mode = state.Mode,
                Start = state.StartUtc.HasValue
                    ? BrazilTime.ConvertUtcToBrazilLocal(state.StartUtc.Value).ToString("yyyy-MM-ddTHH:mm")
                    : null,
                End = state.EndUtc.HasValue
                    ? BrazilTime.ConvertUtcToBrazilLocal(state.EndUtc.Value).ToString("yyyy-MM-ddTHH:mm")
                    : null,
                StatusCode = state.StatusCode,
                Message = state.Message,
                IgnoredPaths = state.IgnoredPaths,
                UpdatedAtUtc = state.UpdatedAtUtc,
                UpdatedBy = state.UpdatedBy
            };
        }
    }
}