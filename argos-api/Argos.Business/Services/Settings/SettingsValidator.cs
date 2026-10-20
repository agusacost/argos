using System.Globalization;
using System.Text.RegularExpressions;
using Argos.Business.Services.Llm;
using Argos.Common.Exceptions;
using Argos.Entities.Dtos;

namespace Argos.Business.Services.Settings;

public sealed partial class SettingsValidator(ILlmService llm) : ISettingsValidator
{
    private const double ThresholdMin = 0.30, ThresholdMax = 0.95;
    private const int PollingMin = 10, PollingMax = 120;
    private const int MaxAlertRecipients = 50;

    public void Validate(SettingsPatchRequest patch)
    {
        var providers = llm.ProviderNames;
        if (patch.LlmProvider is { } provider && !providers.Contains(provider))
            throw new InvalidInputException($"Proveedor LLM inválido: {provider} (opciones: {string.Join(", ", providers)})");
        if (patch.LlmModel is not null && string.IsNullOrWhiteSpace(patch.LlmModel))
            throw new InvalidInputException("El modelo LLM no puede estar vacío");
        if (patch.SimilarityThreshold is { } th && !(th >= ThresholdMin && th <= ThresholdMax))
            throw new InvalidInputException(
                $"Umbral de similitud fuera de rango ({FormatDecimal(ThresholdMin)}–{FormatDecimal(ThresholdMax)})");
        if (patch.PollingIntervalSeconds is { } poll && poll is < PollingMin or > PollingMax)
            throw new InvalidInputException($"Intervalo de refresco fuera de rango ({PollingMin}–{PollingMax} s)");
    }

    public IReadOnlyList<string> NormalizeRecipients(IEnumerable<string?> emails)
    {
        var result = new List<string>();
        var invalid = new List<string>();
        foreach (var raw in emails)
        {
            var email = (raw ?? "").Trim().ToLowerInvariant();
            if (email.Length == 0) continue;
            if (!EmailPattern().IsMatch(email)) invalid.Add(raw!);
            else if (!result.Contains(email)) result.Add(email);
        }
        if (invalid.Count > 0)
            throw new InvalidInputException($"Emails inválidos: {string.Join(", ", invalid)}");
        if (result.Count > MaxAlertRecipients)
            throw new InvalidInputException($"Máximo {MaxAlertRecipients} destinatarios (recibidos {result.Count})");
        return result;
    }

    [GeneratedRegex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$")]
    private static partial Regex EmailPattern();

    /// <summary>Decimal siempre con parte fraccionaria (0.3 → "0.3", 1 → "1.0").</summary>
    private static string FormatDecimal(double d)
    {
        var s = d.ToString("R", CultureInfo.InvariantCulture);
        return s.Contains('.') || s.Contains('E') ? s : s + ".0";
    }
}
