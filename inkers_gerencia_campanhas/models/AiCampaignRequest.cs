namespace Inkers.GerenciadorCampanhas.Models;

public class AiCampaignRequest
{
    public string LojaId           { get; set; } = string.Empty;
    public string Titulo           { get; set; } = string.Empty;
    public string SubTitulo        { get; set; } = string.Empty;
    public string BioArtista       { get; set; } = string.Empty;
    public string Cidade           { get; set; } = string.Empty;
    public string LinkPageStudio   { get; set; } = string.Empty;   

    public decimal OrcamentoMaximo { get; set; }

    public List<string> FotosBase64 { get; set; } = new();
    }