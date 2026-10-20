namespace Argos.Common.Time;

/// <summary>Redondeo de MTTR y métricas.</summary>
public static class RoundingMath
{
    /// <summary>Redondeo a <paramref name="digits"/> decimales, half-to-even (banker's rounding).</summary>
    public static double Round(double x, int digits) => Math.Round(x, digits, MidpointRounding.ToEven);

    /// <summary>División entera redondeada hacia abajo (también para negativos).</summary>
    public static long FloorDiv(long a, long b) => (long)Math.Floor((double)a / b);
}
