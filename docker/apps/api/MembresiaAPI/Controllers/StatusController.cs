using Microsoft.AspNetCore.Mvc;
using MembresiaAPI.Business;

namespace MembresiaAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class StatusController : ControllerBase
    {
        private readonly IUsuarioBusiness _usuarioBusiness;

        public StatusController(IUsuarioBusiness usuarioBusiness)
        {
            _usuarioBusiness = usuarioBusiness;
        }

        [HttpGet]
        public async Task<ActionResult<object>> GetStatus()
        {
            try
            {
                var isConnected = await _usuarioBusiness.TestConnectionAsync();

                return Ok(new
                {
                    status = isConnected ? "OK" : "ERROR",
                    database = isConnected ? "Conectado" : "No conectado",
                    timestamp = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    status = "ERROR",
                    message = $"Error al verificar conexión: {ex.Message}",
                    timestamp = DateTime.UtcNow
                });
            }
        }
    }
}