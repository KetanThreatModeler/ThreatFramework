using Microsoft.AspNetCore.Mvc;
using ThreatFramework.Core.Abstractions;


namespace ThreatFramework.Api.Controllers
{
    [ApiController]
    [Route("index")]
    public sealed class IndexController(IIndexBuilder builder) : ControllerBase
    {
        [HttpPost("build")]
        public async Task<IActionResult> Build([FromQuery] bool rebuild = false, CancellationToken ct = default)
        {
            var (added, total, path) = await builder.BuildOrUpdateAsync(rebuild, ct);
            return Ok(new { added, total, path });
        }

        [HttpGet("healthz")]
        public IActionResult Healthz() => Ok(new { status = "ok" });
    }
}
