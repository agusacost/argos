using MembresiaAPI.Models;

namespace MembresiaAPI.Repository
{
    public interface IUsuarioRepository
    {
        Task<PaginacionResponse<Usuario>> ObtenerUsuariosPaginadoAsync(int pageNumber, int pageSize);
        Task<bool> TestConnectionAsync();
    }
}