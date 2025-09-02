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
        .WithIndentedSequences() // ensure lists under a key are indented
        .ConfigureDefaultValuesHandling(DefaultValuesHandling.OmitDefaults)
        .Build();

    public async Task WriteAsync(IndexDocument doc, string path, CancellationToken ct = default)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);

        var yaml = Serializer.Serialize(doc);

        // Safety net: if (items:\n-) pattern still appears, re-indent dashes.
        // This keeps behavior robust even if library changes.
        yaml = FixUnindentedSequenceAfterItems(yaml);

        var tmp = path + ".tmp";
        await File.WriteAllTextAsync(tmp, yaml, Encoding.UTF8, ct);
        if (File.Exists(path)) File.Delete(path);
        File.Move(tmp, path);
    }

    private static string FixUnindentedSequenceAfterItems(string yaml)
    {
        const string header = "items:\n-";
        if (yaml.Contains(header))
        {
            // Insert two spaces before every dash that immediately follows items:
            // Only adjust the first block right after items: to avoid unintended replacements elsewhere.
            var lines = yaml.Split('\n');
            for (int i = 0; i < lines.Length - 1; i++)
            {
                if (lines[i].TrimEnd() == "items:" && i + 1 < lines.Length && lines[i + 1].StartsWith("-"))
                {
                    // Re-indent subsequent list lines until next top-level key or EOF.
                    for (int j = i + 1; j < lines.Length; j++)
                    {
                        var line = lines[j];
                        if (line.Length == 0) continue;
                        // Stop if we reach another top-level key (non-indented and contains ':')
                        if (!char.IsWhiteSpace(line[0]) && line.Contains(':') && line[0] != '-')
                            break;
                        if (line.StartsWith("-"))
                            lines[j] = "  " + line;
                    }
                    yaml = string.Join('\n', lines);
                    break;
                }
            }
        }
        return yaml;
    }
}
