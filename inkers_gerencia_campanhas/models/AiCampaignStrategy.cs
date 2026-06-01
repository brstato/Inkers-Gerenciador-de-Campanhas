namespace Inkers.GerenciadorCampanhas.Models.AiCampaignStrategy;

using System.Text.Json.Serialization;

public class AiCampaignStrategy
{
    [JsonPropertyName("foco_campanha")]
    public string FocoCampanha { get; set; } = string.Empty;

    [JsonPropertyName("titulo")]
    public List<string> Titulo { get; set; } = new();

    [JsonPropertyName("descricoes")]
    public List<string> Descricoes { get; set; } = new();

    [JsonPropertyName("palavras_chave")]
    public List<string> PalavrasChave { get; set; } = new();

    [JsonPropertyName("orcamento_diario_sugerido")]
    public decimal OrcamentoDiarioSugerido { get; set; }
}