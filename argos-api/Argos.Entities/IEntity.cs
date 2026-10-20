namespace Argos.Entities;

/// <summary>Documento persistido como punto de Qdrant (el id es el id del punto, no va en el payload).</summary>
public interface IEntity
{
    string? Id { get; set; }
}
