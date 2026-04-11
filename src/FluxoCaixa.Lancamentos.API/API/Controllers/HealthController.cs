using Microsoft.AspNetCore.Mvc;

namespace FluxoCaixa.Lancamentos.API.API.Controllers;

/// <summary>
/// Endpoint de saúde do serviço — usado por load balancers e orquestradores.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class HealthController : ControllerBase
{
    /// <summary>Verifica se o serviço está operacional.</summary>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult Get() =>
        Ok(new
        {
            status = "healthy",
            servico = "FluxoCaixa.Lancamentos.API",
            timestamp = DateTime.UtcNow
        });
}
