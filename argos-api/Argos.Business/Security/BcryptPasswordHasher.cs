namespace Argos.Business.Security;

/// <summary>bcrypt <c>$2b$</c> con costo 12 (formato de los hashes guardados en <c>argos_users</c>).</summary>
public sealed class BcryptPasswordHasher : IPasswordHasher
{
    private const int WorkFactor = 12;

    public string Hash(string plain) =>
        BCrypt.Net.BCrypt.HashPassword(plain, BCrypt.Net.BCrypt.GenerateSalt(WorkFactor, 'b'));

    public bool Verify(string plain, string? hash)
    {
        if (string.IsNullOrEmpty(hash)) return false;
        try { return BCrypt.Net.BCrypt.Verify(plain, hash); }
        catch (Exception) { return false; }
    }
}
