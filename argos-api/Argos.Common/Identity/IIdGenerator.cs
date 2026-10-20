namespace Argos.Common.Identity;

/// <summary>Ids de los puntos de Qdrant.</summary>
public interface IIdGenerator
{
    /// <summary>uuid4 aleatorio.</summary>
    string NewId();

    /// <summary>UUID v5 (namespace DNS) del nombre: el mismo nombre siempre da el mismo id.</summary>
    string FromName(string name);
}
