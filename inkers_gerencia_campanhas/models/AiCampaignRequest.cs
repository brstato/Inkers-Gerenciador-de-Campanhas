namespace Inkers.GerenciadorCampanhas.Models.AiCampaignRequest;

public class AiCampaignRequest
{
    public string LojaId { get; set; } = string.Empty;
    public string BioArtista { get; set; } = string.Empty;
    public string Estilo { get; set;} = string.Empty;
    public string Cidade { get; set; } = string.Empty;
    public decimal OrcamentoMaximo { get; set; }
}