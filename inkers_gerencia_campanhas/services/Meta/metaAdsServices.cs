using System.Net.Http.Headers;
using Inkers.GerenciadorCampanhas.Services.Firebird;
using Inkers.GerenciadorCampanhas.Services.Criptografia;
namespace Inkers.GerenciadorCampanhas.Services.Meta;

public class MetaAdsService
{
    private readonly FirebirdRepository _repository;
    private readonly CriptografiaService _cripto;
    private readonly HttpClient _http;

 
    public MetaAdsService(FirebirdRepository repository, CriptografiaService cripto, HttpClient http)
    {
        _repository = repository;
        _cripto = cripto;
        _http = http;
    }

    
    public async Task ProcessarCampanha(string IdLoja)
    {
        try
        {
            var DadosIntegracao = await _repository.ObterIntegracaoMetaAds(IdLoja);

            if (DadosIntegracao == null || string.IsNullOrEmpty(DadosIntegracao.MetaLongToken))
            {
                Console.WriteLine($"Loja {IdLoja} não possui integração com Meta Ads ou token inválido. Pulando sincronização.");
                return;
            }

            string tokenLimpo = _cripto.Descriptografar(DadosIntegracao.MetaLongToken);
            //Console.WriteLine($"{tokenLimpo}");
            string idContaAnuncios = DadosIntegracao.MetaAdAccountId;

            _http.DefaultRequestHeaders.Clear();
            _http.DefaultRequestHeaders.Add("Authorization", "Bearer " + tokenLimpo);
            Console.WriteLine($"Bearer {tokenLimpo}");

            string campos = "campaign_id,campaign_name,spend,clicks,impressions";
            string urlMeta = $"https://graph.facebook.com/v25.0/act_{idContaAnuncios}/insights?level=campaign&fields={campos}&date_preset=last_30d";

            var response = await _http.GetAsync(urlMeta);
            var content = await response.Content.ReadAsStringAsync();

            Console.WriteLine(content);

         
        }

        catch (Exception ex)
        {
            Console.WriteLine($"Erro ao processar campanha para loja {IdLoja}: {ex.Message}");
            return;
        }
     
    }
}