namespace Inkers.GerenciadorCampanhas.Routes.Ai.AiRoutes;

using Inkers.GerenciadorCampanhas.Models;
using Inkers.GerenciadorCampanhas.Services.Ai;
using Inkers.GerenciadorCampanhas.Services.Google;
using Inkers.GerenciadorCampanhas.Services.Google.GoogleAdsBuilderService;
using Microsoft.AspNetCore.Mvc;

/*
 Route: POST /api/campanha/ia/gerar-estrategia

 Fluxo explicado:
 1. O cliente envia um POST com o payload `AiCampaignRequest` contendo informações da loja e parâmetros da campanha.
 2. O route handler valida os campos essenciais (ex.: `LojaId` e `OrcamentoMaximo`).
 3. O `GeminiService` é resolvido via DI e chamado com os parâmetros validados.
 4. `GeminiService` faz chamada à API de IA (Gemini/Generative Language) usando a `ApiKey` configurada e retorna um objeto `AiCampaignStrategy`.
 5. O handler retorna `200 OK` com a estratégia gerada ou um erro apropriado em caso de falha.

 Observações de segurança e implantação:
 - Não logar tokens ou o conteúdo completo do prompt em produção (evitar exposição de dados sensíveis).
 - Use `dotnet user-secrets` ou um vault para configurar `Gemini:ApiKey` em vez de arquivos versionados.
*/
public static class AiRoutes
{
    public static void MapCampanhaIaRoutes(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/campanha/ia");

        // Handler principal: valida request, chama serviço de IA e devolve resultado.
        group.MapPost("/gerar-estrategia", async ([FromBody] AiCampaignRequest request, GeminiService geminiService) =>
        {
            // Validação básica do request — garante parâmetros mínimos antes de chamar a IA.
            if (string.IsNullOrWhiteSpace(request.LojaId))
                return Results.BadRequest("O campo LojaId é obrigatorio.");
            if (request.OrcamentoMaximo < 10)
                return Results.BadRequest("O orçamento máximo deve ser de pelo menos R$10,00 para gerar uma estratégia de campanha.");

            try
            {
                // Chama a camada de IA — responsável por orquestrar a geração do JSON com a estratégia.
                var EstrategiaGerada = await geminiService.GerarEstrategiaCampanha(request);

                // Retorna o objeto desserializado ao cliente.
                return Results.Ok(EstrategiaGerada);
            }
            catch (Exception ex)
            {
                // Em produção, substituir Console.WriteLine por ILogger e evitar expor stack traces para o cliente.
                Console.WriteLine($"[ERRO IA] {ex.Message}");
                return Results.Problem($"Ocorreu um erro ao gerar a estratégia de campanha: {ex.Message}");
            }
        });
    
        group.MapPost("/publicar-campanha", async ([FromBody] PublicarCampanhaRequest request, GoogleAdsBuilderService GoogleService) => 
        {
            if (string.IsNullOrWhiteSpace(request.IdLoja) || request.Estrategia == null)
                return Results.BadRequest("Campos IdLoja e Estrategia são obrigatórios.");

            try
            {
                string ResultadoGoogle = await GoogleService.CriarCampanhaIaAsync(
                    request.IdLoja,
                    request.Estrategia,
                    request.UrlFinalAnuncio
                );

                return Results.Ok(new
                {
                    Mensagem = "Campanha publicada com sucesso no Google Ads!",
                    DetalhesGoogle = ResultadoGoogle
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERRO PUBLICAR] {ex.Message}");  
                return Results.Problem($"Ocorreu um erro ao publicar a campanha: {ex.Message}"); 
            }    
        });
    }
}