using Microsoft.Extensions.Options;
using ThreatFramework.Core.Abstractions;
using ThreatFramework.Core.Index;
using ThreatFramework.Infrastructure.Options;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace ThreatFramework.Infrastructure.Index
{
    public sealed class YamlIndexStore(IOptions<IndexOptions> options) : IIndexStore
    {
        public string Path => options.Value.Path;             

        static ISerializer S = new SerializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .ConfigureDefaultValuesHandling(DefaultValuesHandling.OmitDefaults)
            .Build();

        static IDeserializer D = new DeserializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .IgnoreUnmatchedProperties()
            .Build();

        public async Task<IndexDocument> LoadAsync(CancellationToken ct)
        {
            if (!File.Exists(Path)) return new IndexDocument();
            using var sr = new StreamReader(Path);
            var text = await sr.ReadToEndAsync(ct);
            var doc = D.Deserialize<IndexDocument>(text) ?? new IndexDocument();
            doc.Items = doc.Items.OrderBy(i => i.ShortType).ThenBy(i => i.Id).ToList();
            return doc;
        }

        public async Task SaveAsync(IndexDocument doc, CancellationToken ct)
        {
            Directory.CreateDirectory(System.IO.Path.GetDirectoryName(Path)!);
            doc.Items = doc.Items.OrderBy(i => i.ShortType).ThenBy(i => i.Id).ToList();
            var yaml = S.Serialize(doc);
            var tmp = Path + ".tmp";
            await File.WriteAllTextAsync(tmp, yaml, ct);
            if (File.Exists(Path)) File.Delete(Path);
            File.Move(tmp, Path);
        }
    }
}
