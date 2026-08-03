using MembresiaAPI.Models;
using MembresiaAPI.Repository;

namespace MembresiaAPI.Business
{
    public class UsuarioBusiness : IUsuarioBusiness
    {
        private readonly IUsuarioRepository _usuarioRepository;

        public UsuarioBusiness(IUsuarioRepository usuarioRepository)
        {
            _usuarioRepository = usuarioRepository;
        }

        public async Task<PaginacionResponse<Usuario>> ObtenerUsuariosPaginadoAsync(int pageNumber, int pageSize)
        {
            // Validaciones de negocio
            if (pageNumber < 1)
                pageNumber = 1;

            if (pageSize < 1 || pageSize > 100)
                pageSize = 10;

            return await _usuarioRepository.ObtenerUsuariosPaginadoAsync(pageNumber, pageSize);
        }

        public async Task<bool> TestConnectionAsync()
        {
            return await _usuarioRepository.TestConnectionAsync();
        }
    }
}