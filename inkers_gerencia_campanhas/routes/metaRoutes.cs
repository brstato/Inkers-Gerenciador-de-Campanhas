using Inkers.GerenciadorCampanhas.Services.Meta;

namespace Inkers.GerenciadorCampanhas.Routes;

public static class MetaRoutes
{
    public static void MapMetaRoutes(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/bi/meta");

        group.MapGet("/sync/{idLoja}", async (string idLoja, MetaAdsService metaService) => 
        {

            await metaService.ProcessarCampanha(idLoja);
            return Results.Ok("Meta sincronizada com sucesso!"); 
        });

        group.MapGet("/status", () =>
        {
            return Results.Ok("Meta API está funcionando!");
        });
    }
}