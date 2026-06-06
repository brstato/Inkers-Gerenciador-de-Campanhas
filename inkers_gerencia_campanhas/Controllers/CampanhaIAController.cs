    namespace Inkers.GerenciadorCampanhas.Controllerd.CampanhaIaController;

using Microsoft.AspNetCore.Mvc;
using Inkers.GerenciadorCampanhas.Models;
using Inkers.GerenciadorCampanhas.Services.Ai;
using Inkers.GerenciadorCampanhas.Services.Firebird;


[ApiController]
[Route("api/campanha/ia")]
public class CampanhaIAController : ControllerBase
{
    private readonly GeminiService _geminiServive;

    public CampanhaIAController(GeminiService geminiService)
    {
        _geminiServive = geminiService;
    }

    [HttpPost("gerar-estrategia")]
    public async Task<IActionResult> GerarEstrategia([FromBody] AiCampaignRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.LojaId))
            return BadRequest("O campo LojaId é obrigatório.");
        if (string.IsNullOrWhiteSpace(request.Cidade))
            return BadRequest("O campo Cidade é obrigatório para gerar uma estratégia de campanha.");
        if (request.OrcamentoMaximo < 10)
            return BadRequest("O orçamento máximo deve ser de pelo menos R$10,00 para gerar uma estratégia de campanha.");
        try
        {
            var EstrategiaGerada = await _geminiServive.GerarEstrategiaCampanha(request);
            return Ok(EstrategiaGerada);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ERRO IA] {ex.Message}");
            return StatusCode(500, $"Ocorreu um erro ao gerar a estratégia de campanha: {ex.Message}");
        }
    }
}