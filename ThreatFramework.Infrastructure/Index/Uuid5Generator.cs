
using System.Security.Cryptography;
using System.Text;
using ThreatFramework.Core.Abstractions;
public sealed class Uuid5Generator : IUuid5Generator
{
    public static readonly Guid NamespacePropertyOption = new("9a2a5e4b-7e58-4e52-a9f0-40ab4e97b4c1");

    public Guid FromNamespaceAndName(Guid ns, string name)
    {
        var nsBytes = ns.ToByteArray();
        Swap(nsBytes, 0, 3); Swap(nsBytes, 1, 2); Swap(nsBytes, 4, 5); Swap(nsBytes, 6, 7);

        var nameBytes = Encoding.UTF8.GetBytes(name);
        using var sha1 = SHA1.Create();
        var hash = sha1.ComputeHash([.. nsBytes, .. nameBytes]);

        var g = new byte[16];
        Array.Copy(hash, 0, g, 0, 16);
        g[6] = (byte)((g[6] & 0x0F) | (5 << 4)); // version 5
        g[8] = (byte)((g[8] & 0x3F) | 0x80);     // RFC 4122 variant

        Swap(g, 0, 3); Swap(g, 1, 2); Swap(g, 4, 5); Swap(g, 6, 7);
        return new Guid(g);

        static void Swap(byte[] a, int i, int j) { (a[i], a[j]) = (a[j], a[i]); }
    }
}
