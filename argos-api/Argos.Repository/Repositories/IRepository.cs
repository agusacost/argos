using Argos.Entities;

namespace Argos.Repository.Repositories;

/// <summary>Operaciones comunes sobre una colección de Qdrant que guarda entidades de tipo <typeparamref name="T"/>.</summary>
public interface IRepository<T> where T : Entity
{
    /// <summary>Crea la colección si no existe.</summary>
    Task EnsureCollectionAsync();

    Task<T?> GetByIdAsync(string id);

    Task<IReadOnlyList<T>> ListAsync();

    /// <summary>Inserta o reemplaza el punto completo; asigna el id si la entidad no tiene.</summary>
    Task<T> SaveAsync(T entity);

    /// <summary>Reescribe el payload de una entidad existente.</summary>
    Task UpdateAsync(T entity);

    /// <summary>false si no existía.</summary>
    Task<bool> DeleteAsync(string id);
}
