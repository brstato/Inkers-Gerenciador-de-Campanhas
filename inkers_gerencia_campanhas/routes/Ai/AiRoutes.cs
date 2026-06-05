namespace Inkers.GerenciadorCampanhas.Routes.Ai.AiRoutes;

using Inkers.GerenciadorCampanhas.Models.AiCampaignRequest;
using Inkers.GerenciadorCampanhas.Services.ai.Gemini;
using Microsoft.AspNetCore.Mvc;

public static class AiRoutes
{
    public static void MapCampanhaIaRoutes(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/campanha/ia");
        group.MapPost("/gerar-estrategia", async ([FromBody] AiCampaignRequest request, GeminiService geminiService) =>
        {
            if (string.IsNullOrWhiteSpace(request.LojaId))
                return Results.BadRequest("O campo LojaId é obrigatorio.");  
            if (request.OrcamentoMaximo < 10)
                return Results.BadRequest("O orçamento máximo deve ser de pelo menos R$10,00 para gerar uma estratégia de campanha.");

            try
            {
                var EstrategiaGerada = await geminiService.GerarEstrategiaCampanha(
                    request.BioArtista,
                    request.Estilo,
                    request.Cidade,
                    request.OrcamentoMaximo
                );
                return Results.Ok(EstrategiaGerada);
            }   
            catch (Exception ex)
            {
                Console.WriteLine($"[ERRO IA] {ex.Message}");
                return Results.Problem($"Ocorreu um erro ao gerar a estratégia de campanha: {ex.Message}");
            } 
        });
    }
} 