namespace Inkers.GerenciadorCampanhas.Services.Ai;

using System.Text.Json;
using System.Text;
using Microsoft.Extensions.Configuration;
using Inkers.GerenciadorCampanhas.Models;


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
        AiCampaignRequest request)
    {
        // Fluxo do método GerarEstrategiaCampanha:
        // 1) Lê a ApiKey de configuração (use user-secrets/variáveis de ambiente, não commit).
        // 2) Monta um prompt (system_instruction + user prompt) com regras rígidas para gerar JSON válido.
        // 3) Faz POST para o endpoint de geração e espera a resposta.
        // 4) Extrai o texto retornado e desserializa para o modelo `AiCampaignStrategy`.
        // Observação: a API externa deve retornar apenas o JSON conforme instruído; valide e trate erros de desserialização.

        string ApiKey = _configuration["Gemini:ApiKey"] ?? throw new InvalidCastException("Gemini:ApiKey not configured");
        string Url = $"https://generativelanguage.googleapis.com/v1beta/models/gemini-flash-latest:generateContent?key={ApiKey}";
        string Prompt = @"Você é um especialista em tráfego pago focado EXCLUSIVAMENTE em estúdios de tatuagem.
        Você tem acesso às informações do estúdio e também a imagens reais do portfólio do artista.
        Analise o estilo predominante nas fotos (ex: traço fino, realismo, cores) e use o que você VÊ para criar os textos.

        REGRAS RÍGIDAS:
        1. Crie 3 Títulos (MÁX. 30 CARACTERES CADA).
        2. Crie 2 Descrições (MÁX. 90 CARACTERES CADA). Use a bio do artista como base de tom de voz.
        3. Crie 5 palavras-chave de alta intenção baseadas no que você vê nas fotos e lê no texto.
        4. Defina um foco_campanha curto e o orcamento_diario_sugerido.
        5. RETORNE APENAS O JSON NO FORMATO EXIGIDO. NÃO USE MARKDOWN.
        
        O SEU RETORNO DEVE TER EXATAMENTE ESTA ESTRUTURA E NOME DE CHAVES:
        {
            ""foco_campanha"": ""string"",
            ""titulo"":         [""string"", ""string"", ""string""],
            ""descricoes"":     [""string"", ""string""],
            ""palavras_chave"": [""string"", ""string"", ""string"", ""string"", ""string""],  

            ""orcamento_diario_sugerido"": 0.00
        }";

        string UserPrompt = $@"
            Título da Página:    {request.Titulo         },
            Subtítulo:           {request.SubTitulo      },
            Bio do Artista:      {request.BioArtista     },
            Cidade:              {request.Cidade         },
            Orçamento Máximo: R$ {request.OrcamentoMaximo}";

        var PartList = new List<object> {new { text = UserPrompt } };

        if (request.FotosBase64 != null && request.FotosBase64.Any())
        {
            foreach(var base64 in request.FotosBase64)
            {
                PartList.Add(new {
                        inline_data = new {
                            mime_type = "image/jpg",
                            data = base64
                        }
                    }
                );
            }
        }

        var RequestBody = new
        {
            system_instruction = new { parts = new[] { new { text = Prompt } } },
            contents = new[] {new { parts = PartList } },
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