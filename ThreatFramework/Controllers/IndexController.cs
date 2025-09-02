using Microsoft.AspNetCore.Mvc;
using ThreatFramework.IndexBuilder;

namespace ThreatFramework.Controllers;

[ApiController]
[Route("api/[controller]")]
public class IndexController : ControllerBase
{
    private readonly IIndexBuilder _builder;
    private readonly IIndexWriter _writer;

    public IndexController(IIndexBuilder builder, IIndexWriter writer)
    {
        _builder = builder;
        _writer = writer;
    }

    /// <summary>
    /// Builds an index and returns it as JSON.
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<IndexDocument>> BuildAsync(CancellationToken ct)
    {
        var doc = await _builder.BuildAsync(ct);
        return Ok(doc);
    }

    /// <summary>
    /// Builds an index and writes it to a YAML file on disk, returning the relative path.
    /// </summary>
    /// <param name="fileName">Optional file name (without path). Defaults to index.yaml</param>
    [HttpPost("write")]
    public async Task<ActionResult<object>> BuildAndWriteAsync([FromQuery] string? fileName, CancellationToken ct)
    {
        var doc = await _builder.BuildAsync(ct);
        fileName = string.IsNullOrWhiteSpace(fileName) ? "index.yaml" : fileName.Trim();
        var outputPath = Path.Combine(Directory.GetCurrentDirectory(), fileName);
        await _writer.WriteAsync(doc, outputPath, ct);
        return Ok(new { file = outputPath, count = doc.Items.Count });
    }
}
