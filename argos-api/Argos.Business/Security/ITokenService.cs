using Argos.Entities;

namespace Argos.Business.Security;

/// <summary>Emisión y validación de los JWT de sesión del dashboard.</summary>
public interface ITokenService
{
    string CreateToken(User user);

    /// <summary>Lanza <see cref="Argos.Common.Exceptions.UnauthorizedException"/> ("Token inválido" / "Token expirado").</summary>
    CurrentUser ValidateToken(string token);
}
