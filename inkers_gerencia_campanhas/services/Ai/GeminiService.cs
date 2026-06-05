namespace Inkers.GerenciadorCampanhas.Services.ai.Gemini;

using System.Text.Json;
using System.Text;
using Microsoft.Extensions.Configuration;
using Inkers.GerenciadorCampanhas.Models.AiCampaignStrategy;
using Inkers.GerenciadorCampanhas.Models.AiCampaignRequest;

public class GeminiService
{
    private readonly HttpClient _http;
    private readonly IConfiguration _configuration;

    public GeminiService(HttpClient http, IConfiguration configuration)
    {
        _http = http;
        _configuration = configuration;
    }

    public async Task<AiCampaignStrategy> GerarEstrategiaCampanha(
        string BioArtista, string Estilos, string Cidade, decimal OrcamentoMaximo)
    {
        string ApiKey = _configuration["Gemini:ApiKey"] ?? throw new InvalidCastException("Gemini:ApiKey not configured");
        string Url = $"https://generativelanguage.googleapis.com/v1beta/models/gemini-flash-latest:generateContent?key={ApiKey}";
        string Prompt = @"Você é um especialista em tráfego pago e marketing digital de alta performance, 
        focado EXCLUSIVAMENTE em estúdios de tatuagem.
        Sua tarefa é ler as informações do tatuador e criar uma estrutura de campanha para a 
        Rede de Pesquisa do Google Ads.

        REGRAS RÍGIDAS:
        1. Crie 3 Títulos persuasivos (MÁXIMO 30 CARACTERES CADA).
        2. Crie 2 Descrições detalhadas (MÁXIMO 90 CARACTERES CADA).
        3. Crie 5 palavras-chave de alta intenção de compra focadas na especialidade informada.
        4. Defina um resumo curto para o foco_campanha e defina o orcamento_diario_sugerido baseado no valor informado pelo usuário.
        5. RETORNE APENAS UM JSON VÁLIDO. NÃO INCLUA TEXTO FORA DO JSON. NÃO USE MARKDOWN (```json).

        O SEU RETORNO DEVE TER EXATAMENTE ESTA ESTRUTURA E NOME DE CHAVES:
        {
            ""foco_campanha"": ""string"",
            ""titulo"": [""string"", ""string"", ""string""],
            ""descricoes"": [""string"", ""string""],
            ""palavras_chave"": [""string"", ""string"", ""string"", ""string"", ""string""],
            ""orcamento_diario_sugerido"": 0.00
        }";

        string UserPrompt = $"Bio do Artista: {BioArtista}\n Especialidades: {Estilos}\n Cidade/Região de atuação: {Cidade}\n Orçamento Diário Disponível: R$ {OrcamentoMaximo}";

        var RequestBody = new
        {
            system_instruction = new { parts = new[] { new { text = Prompt } } },
            contents = new[]
            {
                new { parts = new[] { new { text = UserPrompt } } }
            },
            generationConfig = new
            {
                temperature = 0.7, 
                response_mime_type = "application/json" 
            }
        };

        string JsonBody = JsonSerializer.Serialize(RequestBody);
        using var RequestMessage = new HttpRequestMessage(HttpMethod.Post, Url);
        RequestMessage.Content = new StringContent(JsonBody, Encoding.UTF8, "application/json");

        var Response = await _http.SendAsync(RequestMessage);

        if (!Response.IsSuccessStatusCode)
        {
            string Error = await Response.Content.ReadAsStringAsync();
            Console.WriteLine($"Erro ao gerar estratégia de campanha: {Response.StatusCode} - {Error}");
            throw new Exception($"Erro na API Gemini: {Response.StatusCode} - {Error}");
        }

        string JsonResponse = await Response.Content.ReadAsStringAsync();

        using var JsonDoc = JsonDocument.Parse(JsonResponse);
        string TextResult = JsonDoc.RootElement
            .GetProperty("candidates")[0]
            .GetProperty("content")
            .GetProperty("parts")[0]
            .GetProperty("text")
            .GetString() ?? throw new Exception("Resposta da Gemini não contém texto.");

        var Estrategia = JsonSerializer.Deserialize<AiCampaignStrategy>(TextResult);

        return Estrategia ?? throw new Exception("Falha ao desserializar a estratégia de campanha. Verifique o formato do JSON retornado pela Gemini.");    
    }


}