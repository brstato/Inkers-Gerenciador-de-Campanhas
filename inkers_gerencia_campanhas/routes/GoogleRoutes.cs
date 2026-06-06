namespace Inkers.GerenciadorCampanhas.Routes.Google;

using Inkers.GerenciadorCampanhas.Services.Google;

/*
 Route: GET /api/google/sync/{idloja}

 Fluxo:
 - Recebe o `idloja` na URL.
 - Resolve `GoogleAdsService` via DI e chama `ProcessarCampanha(idLoja)`.
 - `GoogleAdsService`:
    1) consulta `FirebirdRepository` para obter credenciais/refresh token;
    2) obtém `access_token` via fluxo OAuth (refresh_token -> access_token);
    3) monta e envia requisição para Google Ads API;
    4) processa/loga a resposta e retorna status apropriado.

 Observações:
 - O handler atual não retorna explicitamente um `Results.Ok()` quando tudo dá certo (padrão é 200/204 dependendo do pipeline). Considere retornar conteúdo ou status to reflect outcome.
 - Use `ILogger<T>` em vez de `Console.WriteLine` para mensagens estruturadas.
*/
public static class GoogleRoutes
{
    public static void GoogleRoutesMap(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/google");

        group.MapGet("/sync/{idloja}", async (string IdLoja, GoogleAdsService GoogleService) =>
        {
           // Chama o serviço responsável por processar/ler a integração do Google para a loja.
           await GoogleService.ProcessarCampanha(IdLoja);
           return Results.Ok();
        });
    }
}