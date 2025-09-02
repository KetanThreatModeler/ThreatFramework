using Microsoft.AspNetCore.Mvc;
using ThreatFramework.Infrastructure.Cache;

namespace ThreatFramework.Controllers;

[ApiController]
[Route("api/index-cache")] 
public sealed class IndexCacheController : ControllerBase
{
    private readonly IIndexCache _cache;

    public IndexCacheController(IIndexCache cache)
    {
        _cache = cache;
    }

    /// <summary>Gets the numeric Id for a given kind and guid if present.</summary>
    [HttpGet("resolve/{kind}/{guid}")]
    public ActionResult<object> Resolve(string kind, Guid guid)
    {
        if (_cache.TryGetId(kind, guid, out var id))
            return Ok(new { kind, guid, id, version = _cache.Version });
        return NotFound(new { kind, guid, message = "Not found" });
    }

    /// <summary>Forces a refresh of the underlying index cache.</summary>
    [HttpPost("refresh")]
    public async Task<ActionResult<object>> Refresh(CancellationToken ct)
    {
        await _cache.RefreshAsync(ct);
        return Ok(new { status = "refreshed", version = _cache.Version });
    }
}
