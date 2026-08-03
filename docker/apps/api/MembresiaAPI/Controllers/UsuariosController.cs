using Microsoft.AspNetCore.Mvc;
using MembresiaAPI.Business;
using MembresiaAPI.Models;

namespace MembresiaAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UsuariosController : ControllerBase
    {
        private readonly IUsuarioBusiness _usuarioBusiness;

        public UsuariosController(IUsuarioBusiness usuarioBusiness)
        {
            _usuarioBusiness = usuarioBusiness;
        }

        [HttpGet]
        public async Task<ActionResult<PaginacionResponse<Usuario>>> GetUsuarios([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10)
        {
            try
            {
                var result = await _usuarioBusiness.ObtenerUsuariosPaginadoAsync(pageNumber, pageSize);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"Error al obtener usuarios: {ex.Message}" });
            }
        }
    }
}