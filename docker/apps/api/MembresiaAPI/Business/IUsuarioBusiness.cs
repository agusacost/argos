using MembresiaAPI.Models;

namespace MembresiaAPI.Business
{
    public interface IUsuarioBusiness
    {
        Task<PaginacionResponse<Usuario>> ObtenerUsuariosPaginadoAsync(int pageNumber, int pageSize);
        Task<bool> TestConnectionAsync();
    }
}