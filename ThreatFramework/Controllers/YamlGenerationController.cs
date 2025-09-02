using Microsoft.AspNetCore.Mvc;
using ThreatFramework.Utils.YamlFileWriter.Generation;

namespace ThreatFramework.Controllers;

[ApiController]
[Route("api/yaml/export")] 
public sealed class YamlGenerationController : ControllerBase
{
    private readonly ICompositeYamlGenerationService _composite;

    public YamlGenerationController(ICompositeYamlGenerationService composite)
    {
        _composite = composite;
    }

    /// <summary>
    /// Generates YAML files for threats, components, security requirements and libraries.
    /// </summary>
    /// <param name="rootDir">Root directory to place per-kind subfolders. Defaults to 'export'.</param>
    [HttpPost]
    public async Task<ActionResult<object>> ExportAll([FromQuery] string? rootDir, CancellationToken ct)
    {
        var dir = string.IsNullOrWhiteSpace(rootDir) ? "export" : rootDir.Trim();
        var full = Path.GetFullPath(dir);
        var results = await _composite.GenerateAllAsync(full, ct);
        return Ok(new { root = full, results });
    }
}
