namespace Argos.Entities;

/// <summary>Validación de key + modelo: <see cref="Error"/> bloquea el guardado, <see cref="Warning"/> solo avisa.</summary>
public sealed record LlmCheckResult(string? Error, string? Warning)
{
    public static readonly LlmCheckResult Ok = new(null, null);

    public static LlmCheckResult Failed(string error) => new(error, null);

    public static LlmCheckResult Warn(string warning) => new(null, warning);
}
