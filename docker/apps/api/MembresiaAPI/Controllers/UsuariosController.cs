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
        private readonly ILogger<UsuariosController> _logger;

        public UsuariosController(IUsuarioBusiness usuarioBusiness, ILogger<UsuariosController> logger)
        {
            _usuarioBusiness = usuarioBusiness;
            _logger = logger;
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
                // El error se registra además de devolverse: sin esta línea la API respondía 500 sin dejar
                // rastro en sus logs y quien diagnosticaba (una persona o Argos) no podía ver la causa.
                _logger.LogError(ex, "GET /api/usuarios falló: {Causa}", (ex.InnerException ?? ex).Message);
                return StatusCode(500, new { message = $"Error al obtener usuarios: {ex.Message}" });
            }
        }
    }
}