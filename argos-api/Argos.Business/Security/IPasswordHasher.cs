namespace Argos.Business.Security;

public interface IPasswordHasher
{
    string Hash(string plain);

    /// <summary>false si el hash falta o es inválido.</summary>
    bool Verify(string plain, string? hash);
}
