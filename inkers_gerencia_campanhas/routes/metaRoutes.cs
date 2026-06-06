using Inkers.GerenciadorCampanhas.Services.Meta;

namespace Inkers.GerenciadorCampanhas.Routes;

/*
 Routes para integração com Meta (Facebook/Meta Ads)

 Fluxo do endpoint `/api/bi/meta/sync/{idLoja}`:
 1. O handler recebe `idLoja` e resolve `MetaAdsService` via DI.
 2. `MetaAdsService.ProcessarCampanha`:
    - busca credenciais no `FirebirdRepository`;
    - descriptografa o token via `CriptografiaService` (se aplicável);
    - monta requisição à API da Meta com `Authorization: Bearer <token>`;
    - processa a resposta e faz log/retorno.

 Observações:
 - Este handler retorna `Results.Ok(...)` após chamar o serviço; em cenários reais, verifique e propague erros quando necessário.
 - Evite expor tokens ou respostas completas em logs de produção.
*/
public static class MetaRoutes
{
    public static void MapMetaRoutes(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/bi/meta");

        group.MapGet("/sync/{idLoja}", async (string idLoja, MetaAdsService metaService) => 
        {
            // Delegamos toda a lógica de integração ao serviço específico.
            await metaService.ProcessarCampanha(idLoja);
            return Results.Ok("Meta sincronizada com sucesso!"); 
        });

        // Health check simples para confirmar que o módulo Meta está ativo.
        group.MapGet("/status", () =>
        {
            return Results.Ok("Meta API está funcionando!");
        });
    }
}