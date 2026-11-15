using System.Globalization;
using System.Net;
using System.Net.Sockets;
using System.Text.RegularExpressions;

namespace Argos.Business.Services.Privacy;

/// <summary>
/// Reemplaza cada dirección IP (IPv4 e IPv6) por un marcador <c>[IP-n]</c> estable dentro de la sesión y
/// los restaura después.
/// <para>
/// Falsos positivos conocidos (riesgo aceptado): una secuencia de cuatro números de 0 a 255 separados por puntos y
/// sin una letra pegada adelante (p. ej. "versión 1.2.3.4") se trata como IPv4. "v1.2.3.4", "1.2.3" y
/// "1.2.3.4.5" no se tocan. En IPv6 solo se aceptan candidatos que <see cref="IPAddress.TryParse(string, out IPAddress)"/>
/// valida como IPv6 (las horas "10:20:30" o las MAC "aa:bb:cc:dd:ee:ff" no lo son). Como la respuesta se restaura,
/// un falso positivo no le cambia el texto al operador: solo oculta ese número al proveedor.
/// </para>
/// </summary>
public sealed partial class IpPseudonymizationSession
{
    /// <summary>Aclaración que se agrega al system prompt cuando se reemplazó al menos una IP.</summary>
    public const string ModelNotice =
        " Por privacidad, las direcciones IP del texto se reemplazaron por marcadores como [IP-1]: " +
        "si necesitás mencionar una IP, escribí el marcador exactamente igual.";

    private readonly Dictionary<string, string> _markerByIp = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<int, string> _ipByNumber = [];

    public IpPseudonymizationSession(bool enabled)
    {
        IsEnabled = enabled;
    }

    public bool IsEnabled { get; }

    /// <summary>Cantidad de direcciones distintas reemplazadas en la sesión.</summary>
    public int Count => _ipByNumber.Count;

    /// <summary>Texto con las IP reemplazadas; la misma IP recibe siempre el mismo marcador.</summary>
    public string Mask(string text)
    {
        if (!IsEnabled || string.IsNullOrEmpty(text)) return text;
        // Primero IPv6: así una IPv4 embebida (::ffff:10.0.0.1) se toma como parte de la dirección completa.
        var masked = Ipv6Candidate().Replace(text, m => MaskIpv6(m.Value));
        return Ipv4().Replace(masked, m => MarkerFor(m.Value));
    }

    /// <summary>System y user de una misma llamada, con marcadores compartidos y la aclaración para el modelo.</summary>
    public (string System, string User) MaskPrompts(string systemPrompt, string userPrompt)
    {
        var user = Mask(userPrompt);
        var system = Mask(systemPrompt);
        return (Count > 0 ? system + ModelNotice : system, user);
    }

    /// <summary>Vuelve a poner las IP reales; los marcadores que no son de esta sesión quedan como están.</summary>
    public string Restore(string text)
    {
        if (Count == 0 || string.IsNullOrEmpty(text)) return text;
        return Marker().Replace(text, m =>
            int.TryParse(m.Groups[1].Value, NumberStyles.None, CultureInfo.InvariantCulture, out var n) &&
            _ipByNumber.TryGetValue(n, out var ip)
                ? ip
                : m.Value);
    }

    private string MaskIpv6(string candidate)
    {
        // El candidato puede arrastrar un ':' final ("fe80::1: error"): se conserva fuera del marcador.
        var trailing = "";
        var address = candidate;
        while (address.Length > 0 && !IsIpv6(address) && address.EndsWith(':'))
        {
            trailing = ":" + trailing;
            address = address[..^1];
        }
        return IsIpv6(address) ? MarkerFor(address) + trailing : candidate;
    }

    private static bool IsIpv6(string value) =>
        value.Any(Uri.IsHexDigit) &&
        IPAddress.TryParse(value, out var ip) && ip.AddressFamily == AddressFamily.InterNetworkV6;

    private string MarkerFor(string ip)
    {
        if (_markerByIp.TryGetValue(ip, out var marker)) return marker;
        var number = _ipByNumber.Count + 1;
        marker = $"[IP-{number.ToString(CultureInfo.InvariantCulture)}]";
        _markerByIp[ip] = marker;
        _ipByNumber[number] = ip;
        return marker;
    }

    // Cuatro octetos 0-255; sin letra/dígito/punto pegado adelante ni otro ".dígito" o letra atrás.
    [GeneratedRegex(@"(?<![\w.])(?:(?:25[0-5]|2[0-4]\d|1\d\d|[1-9]?\d)\.){3}(?:25[0-5]|2[0-4]\d|1\d\d|[1-9]?\d)(?!\.?\d)(?!\w)")]
    private static partial Regex Ipv4();

    // Candidato IPv6 (grupos hex con ':', IPv4 embebida y zona opcionales); se valida con IPAddress.TryParse.
    [GeneratedRegex(@"(?<![\w:.\-])(?:[0-9A-Fa-f]{0,4}:){2,7}(?:(?:\d{1,3}\.){3}\d{1,3}|[0-9A-Fa-f]{1,4})?(?:%[0-9A-Za-z_.]+)?(?![\w:])")]
    private static partial Regex Ipv6Candidate();

    [GeneratedRegex(@"\[IP-(\d{1,6})\]", RegexOptions.IgnoreCase)]
    private static partial Regex Marker();
}
