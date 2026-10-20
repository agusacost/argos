namespace Argos.Common.Time;

/// <summary>Hora actual en los formatos ISO 8601 que usan los datos guardados en Qdrant.</summary>
public interface IClock
{
    DateTimeOffset UtcNow { get; }

    /// <summary>UTC sin offset (p.ej. <c>2026-10-19T12:00:00.123456</c>).</summary>
    string UtcNowNaive();

    /// <summary>UTC con sufijo <c>+00:00</c>.</summary>
    string UtcNowAware();
}
