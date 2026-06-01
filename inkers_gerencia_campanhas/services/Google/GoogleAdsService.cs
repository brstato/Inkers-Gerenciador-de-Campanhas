namespace Inkers.GerenciadorCampanhas.Services.Google;

using Inkers.GerenciadorCampanhas.Models;
using Inkers.GerenciadorCampanhas.Services.Firebird;
using System.Text.Json;
using System.Net.Http.Headers;
using Microsoft.Extensions.Configuration;
using System.Text;

public class GoogleAdsService
{
    private readonly FirebirdRepository _repository;
    private readonly HttpClient _http;
    private readonly IConfiguration _configuration;

    public GoogleAdsService(FirebirdRepository repository, HttpClient http, IConfiguration configuration)
    {
        _repository = repository;
        _http = http;
        _configuration = configuration;
    }

    public async Task ProcessarCampanha(string IdLoja)
    {
        try
        {
            var DadosIntegracao = await _repository.ObterIntegracaoGoogleAds(IdLoja);

            if (DadosIntegracao == null || string.IsNullOrEmpty(DadosIntegracao.GoogleAdsId))
            {
                Console.WriteLine($"Loja {IdLoja} não possui integração com Google Ads ou token inválido. Pulando sincronização.");
                return;
            }

            string _IdContaAnuncios = DadosIntegracao.GoogleAdsId;
            string _RefreshToken = DadosIntegracao.GoogleRefreshToken;
            string _GoogleAnalyticsId = DadosIntegracao.GoogleAnalyticsId;
            string _IdLoja = DadosIntegracao.IdLoja;

            string AccessToken = await GerarAccessToken(_RefreshToken);
            string UrlGoogle = $"https://googleads.googleapis.com/v20/customers/{_IdContaAnuncios}/googleAds:searchStream";

            string Query = "SELECT campaign.id, campaign.name FROM campaign ORDER BY campaign.id";
            var RequestBody = new { query = Query};
            string JsonBody = JsonSerializer.Serialize(RequestBody);
            using var RequestMessage = new HttpRequestMessage(HttpMethod.Post, UrlGoogle);
            RequestMessage.Content = new StringContent(JsonBody, Encoding.UTF8, "application/json");

            RequestMessage.Headers.Authorization = new AuthenticationHeaderValue("Bearer", AccessToken);
            RequestMessage.Headers.Add("developer-token", _configuration["GoogleAds:DeveloperToken"] ?? throw new InvalidOperationException("GoogleAds:DeveloperToken not configured"));

            var Response = await _http.SendAsync(RequestMessage);

            if (!Response.IsSuccessStatusCode)
            {
                string ErrorContent = await Response.Content.ReadAsStringAsync();
                Console.WriteLine($"Erro ao obter campanhas para loja {IdLoja}: {Response.StatusCode} - {ErrorContent}");
                return;
            }

            string JsonResponse = await Response.Content.ReadAsStringAsync();
            Console.WriteLine($"Resposta do Google Ads para loja {IdLoja}: {JsonResponse}");

            _http.DefaultRequestHeaders.Clear();
            _http.DefaultRequestHeaders.Add("Authorization", "Bearer " + _RefreshToken);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Erro ao processar campanha para loja {IdLoja}: {ex.Message}");
            return;
        }
    }


    private async Task<string> GerarAccessToken(string RefreshToken)
    {
        var Parametros = new Dictionary<string, string>
        {
          {"client_id", _configuration["GoogleAds:ClientId"] ?? throw new InvalidOperationException("GoogleAds:ClientId not configured") },
          {"client_secret", _configuration["GoogleAds:ClientSecret"] ?? throw new InvalidOperationException("GoogleAds:ClientSecret not configured") },
          {"refresh_token", RefreshToken },
          {"grant_type", "refresh_token" }
        };

        var RequestConent = new FormUrlEncodedContent(Parametros);
        var TokenResponse = await _http.PostAsync("https://oauth2.googleapis.com/token", RequestConent);

        if (!TokenResponse.IsSuccessStatusCode)
            throw new Exception($"Erro ao obter access token: {TokenResponse.StatusCode} - {await TokenResponse.Content.ReadAsStringAsync()}");

        string JsonResponse = await TokenResponse.Content.ReadAsStringAsync();
        using var JsonDoc = JsonDocument.Parse(JsonResponse);    

        var accessToken = JsonDoc.RootElement.GetProperty("access_token").GetString();
        if (string.IsNullOrEmpty(accessToken))
            throw new Exception("Access token not found in response.");
        return accessToken;
    }
}