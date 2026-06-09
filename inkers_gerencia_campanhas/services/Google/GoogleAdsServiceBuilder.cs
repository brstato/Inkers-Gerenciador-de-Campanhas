namespace Inkers.GerenciadorCampanhas.Services.Google.GoogleAdsBuilderService;

using System.Text.Json;
using System.Text;
using System.Net.Http.Headers;
using Microsoft.Extensions.Configuration;
using Inkers.GerenciadorCampanhas.Services.Firebird;
using Inkers.GerenciadorCampanhas.Models;

public class GoogleAdsBuilderService
{
    private readonly FirebirdRepository _repository;
    private readonly HttpClient _http;
    private readonly IConfiguration _configuration;

    public GoogleAdsBuilderService(FirebirdRepository repository, HttpClient http, IConfiguration configuration)
    {
        _repository = repository;
        _http = http;
        _configuration = configuration;
    }

    public async Task<string> CriarCampanhaIaAsync(string IdLoja, AiCampaignStrategy estrategia, string urlFinal)
    {
        var Credenciais = await _repository.ObterIntegracaoGoogleAds(IdLoja)
            ?? throw new InvalidOperationException($"Loja {IdLoja} não possui integração com Google Ads ou token inválido.");

        string CustomerId = Credenciais.GoogleAdsId;
        string AccessToken = await GerarAccessToken(Credenciais.GoogleRefreshToken);
        string urlBase = $"https://googleads.googleapis.com/v20/customers/{CustomerId}/googleAds:mutate";
        long OrcamentoMicros = (long)(estrategia.OrcamentoDiarioSugerido * 1000000);
        var Operations = new List<object>
        {
            new
            {
                campaignBudgetOperation = new
                {
                    create = new
                    {
                        resourceName = $"customers/{CustomerId}/campaignBudgets/-1",
                        name = $"Orçamento Inkers IA - {DateTime.Now:dd/MM/yyyy HH:mm}",
                        amountMicros = OrcamentoMicros,
                        deliveryMethod = "STANDARD"                        
                    }
                }
            },
            new {
                campaignOperation = new {
                    create = new {
                        resourceName = $"customers/{CustomerId}/campaigns/-2",
                        name = $"Campanha Gerada Inkers - {estrategia.FocoCampanha}",
                        status = "PAUSED",
                        advertisingChannelType = "SEARCH",
                        campaignBudget = $"customers/{CustomerId}/campaignBudgets/-1",
                        targetSpend = new { },
                        containsEuPoliticalAdvertising = "DOES_NOT_CONTAIN_EU_POLITICAL_ADVERTISING",
                        networkSettings = new {
                            targetGoogleSearch = true,
                            targetSearchNetwork = true
                        }
                    }
                }
            },   
            new {
                adGroupOperation = new {
                    create = new {
                        resourceName = $"customers/{CustomerId}/adGroups/-3",
                        name = "Grupo Principal - IA",
                        status = "ENABLED",
                        campaign = $"customers/{CustomerId}/campaigns/-2",
                        type = "SEARCH_STANDARD"
                    }
                }
            }                     
        };  

        foreach (var palavra in estrategia.PalavrasChave)
        {
            Operations.Add(new {
                adGroupCriterionOperation = new {
                    create = new {
                        adGroup = $"customers/{CustomerId}/adGroups/-3",
                        status = "ENABLED",
                        keyword = new {
                            text = palavra,
                            matchType = "PHRASE" 
                        }
                    }
                }
            });            
        }

        var PalavrasNegativas = new List<string> 
        {
            "curso", "workshop", "aprender", "aula", "tutorial",
            "máquina", "tinta", "agulha", "kit", "material",
            "henna", "temporária", "falsa", "adesivo",
            "remoção", "laser", "apagar", "tirar",
            "grátis", "barato", "significado", "caseira"
        };    

        foreach (var palavra in PalavrasNegativas)
        {
            Operations.Add(new {
                campaignCriterionOperation = new {
                    create = new {
                        campaign = $"customers/{CustomerId}/campaigns/-2",
                        negative = true,
                        keyword = new {
                            text = palavra,
                            matchType = "BROAD"
                        }
                    }
                }
            });            
        }                

        var TitulosFormatados = estrategia.Titulo.Select(t => new { text = t}).ToList();
        var DescricoesFormatadas = estrategia.Descricoes.Select(d => new { text = d}).ToList();

        Operations.Add(new
        {
            adGroupAdOperation = new
            {
                create = new
                {
                    adGroup = $"customers/{CustomerId}/adGroups/-3",
                    status = "ENABLED",
                    ad = new
                    {
                        finalUrls = new[] { urlFinal },
                        responsiveSearchAd = new
                        {
                            headlines = TitulosFormatados,
                            descriptions = DescricoesFormatadas
                        }
                    }
                }
            }
        });    

        var RequestBody = new { mutateOperations = Operations };

        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };        

        string JsonBody  = JsonSerializer.Serialize(RequestBody, options);

        using var RequestMessage = new HttpRequestMessage(HttpMethod.Post, urlBase);
        RequestMessage.Content = new StringContent(JsonBody, Encoding.UTF8, "application/json");
        RequestMessage.Headers.Authorization = new AuthenticationHeaderValue("Bearer", AccessToken);
        RequestMessage.Headers.Add("developer-token", _configuration["GoogleAds:DeveloperToken"]);
        RequestMessage.Headers.Add("login-customer-id", _configuration["GoogleAds:LoginCustomerId"]);
        
        var Response  = await _http.SendAsync(RequestMessage);

        string ResponseContent = await Response.Content.ReadAsStringAsync();

        if (!Response.IsSuccessStatusCode)
        {
            Console.WriteLine($"Erro ao criar campanha: {ResponseContent}");
            throw new InvalidOperationException("Erro ao criar campanha no Google Ads.");
        }

        return ResponseContent;
    }

    private async Task<string> GerarAccessToken(string RefreshToken)
    {
        var Parametros = new Dictionary<string, string>
        {
            {"client_id",     _configuration["GoogleAds:ClientId"]!},
            {"client_secret", _configuration["GoogleAds:ClientSecret"]!},
            {"refresh_token", RefreshToken},
            {"grant_type",   "refresh_token"}
        };

        var RequestToken = new FormUrlEncodedContent(Parametros);
        var TokenResponse = await _http.PostAsync("https://oauth2.googleapis.com/token", RequestToken);

        if (!TokenResponse.IsSuccessStatusCode)
        {
            string erroDetalhado = await TokenResponse.Content.ReadAsStringAsync();
            throw new InvalidOperationException($"Erro ao gerar token de acesso: {erroDetalhado}"); 
        }

        string JsonResponse = await TokenResponse.Content.ReadAsStringAsync();
        using var JsonDoc = JsonDocument.Parse(JsonResponse);
        return JsonDoc.RootElement.GetProperty("access_token").GetString()!;
    }


    public async Task<(bool TemSaldo, string GoogleAdsID)> VerificarSaldoAsync(string IdLoja)
    {
        var Credenciais = await _repository.ObterIntegracaoGoogleAds(IdLoja)
            ?? throw new InvalidOperationException("Loja não possui integração com Google Ads ou token inválido.");

        string CustomerId = Credenciais.GoogleAdsId;
        string AccessToken = await GerarAccessToken(Credenciais.GoogleRefreshToken);

        string urlSearch = $"https://googleads.googleapis.com/v20/customers/{CustomerId}/googleAds:search";    

        var RequestBody = new
        {
            query = "SELECT billing_setup.id, billing_setup.status FROM billing_setup WHERE billing_setup.status = 'APPROVED'"
        };        

        string jsonBody = JsonSerializer.Serialize(RequestBody);

        using var RequestMessage = new HttpRequestMessage(HttpMethod.Post, urlSearch);
        RequestMessage.Content = new StringContent(jsonBody, Encoding.UTF8, "application/json");
        RequestMessage.Headers.Authorization = new AuthenticationHeaderValue("Bearer", AccessToken);
        RequestMessage.Headers.Add("developer-token", _configuration["GoogleAds:DeveloperToken"]);
        RequestMessage.Headers.Add("login-customer-id", _configuration["GoogleAds:LoginCustomerId"]);

        var Response = await _http.SendAsync(RequestMessage);

        if (!Response.IsSuccessStatusCode)
        {
            Console.WriteLine($"Erro ao verificar saldo: {Response.StatusCode}");
            throw new InvalidOperationException($"Erro ao verificar saldo: {Response.StatusCode}");
        }

        using var JsonDoc = JsonDocument.Parse(await Response.Content.ReadAsStringAsync());

        bool TemSaldo = JsonDoc.RootElement.TryGetProperty("results", out _);
        return (TemSaldo, Credenciais.GoogleAdsId);
    }
}