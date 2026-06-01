namespace Inkers.GerenciadorCampanhas.Routes.Google;

using Inkers.GerenciadorCampanhas.Services.Google;

public static class GoogleRoutes
{
    public static void GoogleRoutesMap(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/google");

        group.MapGet("/sync/{idloja}", async (string IdLoja, GoogleAdsService GoogleService) =>
        {
           await GoogleService.ProcessarCampanha(IdLoja); 
        });
    }
} 