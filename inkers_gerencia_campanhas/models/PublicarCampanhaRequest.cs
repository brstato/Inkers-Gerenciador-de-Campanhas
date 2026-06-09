namespace Inkers.GerenciadorCampanhas.Models;

using Inkers.GerenciadorCampanhas.Models;

public class PublicarCampanhaRequest
{
    public string IdLoja { get; set; } = string.Empty;
    public string UrlFinalAnuncio { get; set; } = string.Empty;
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public AiCampaignStrategy Estrategia { get; set; } = new();
}