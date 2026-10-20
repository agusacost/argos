using System.Security.Cryptography;
using System.Text;

namespace Argos.Common.Identity;

public sealed class UuidGenerator : IIdGenerator
{
    private static readonly Guid NamespaceDns = Guid.Parse("6ba7b810-9dad-11d1-80b4-00c04fd430c8");

    public string NewId() => Guid.NewGuid().ToString("D");

    /// <summary>RFC 4122 versión 5 (SHA-1) sobre el namespace DNS.</summary>
    public string FromName(string name)
    {
        var ns = NamespaceDns.ToByteArray(bigEndian: true);
        var nameBytes = Encoding.UTF8.GetBytes(name);
        var hash = SHA1.HashData([.. ns, .. nameBytes]);
        var b = hash[..16];
        b[6] = (byte)((b[6] & 0x0F) | 0x50);
        b[8] = (byte)((b[8] & 0x3F) | 0x80);
        return new Guid(b, bigEndian: true).ToString("D");
    }
}
