using System.Text;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace ThreatFramework.IndexBuilder;

public interface IIndexWriter
{
    Task WriteAsync(IndexDocument doc, string path, CancellationToken ct = default);
}

public sealed class YamlIndexWriter : IIndexWriter
{
    private static readonly ISerializer Serializer = new SerializerBuilder()
        .WithNamingConvention(CamelCaseNamingConvention.Instance)
        .ConfigureDefaultValuesHandling(DefaultValuesHandling.OmitDefaults)
        .Build();

    public async Task WriteAsync(IndexDocument doc, string path, CancellationToken ct = default)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        var yaml = Serializer.Serialize(doc);
        var tmp = path + ".tmp";
        await File.WriteAllTextAsync(tmp, yaml, Encoding.UTF8, ct);
        if (File.Exists(path)) File.Delete(path);
        File.Move(tmp, path);
    }
}
