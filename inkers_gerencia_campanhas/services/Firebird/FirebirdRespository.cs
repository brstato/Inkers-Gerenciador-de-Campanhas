namespace Inkers.GerenciadorCampanhas.Services.Firebird;

using System.Data;
using FirebirdSql.Data.FirebirdClient;
using Dapper;
using Inkers.GerenciadorCampanhas.Models;
using Microsoft.Extensions.Configuration;
using System.Threading.Tasks;


public class FirebirdRepository
{
    private readonly string _connectionString;

    public FirebirdRepository(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("FirebirdConnection") ??
            throw new ArgumentException("Connection string 'FirebirdConnection' not found.");
    }

    public async Task<IntegracaoAds?> ObterIntegracaoMetaAds(string Idloja)
    {
        using IDbConnection conexao = new FbConnection(_connectionString);

        string sql = @"
            select 
                uuid as LojaId, 
                meta_long_token as MetaLongToken, 
                meta_pixel_id as MetaAdAccountId
            from loja where uuid = @IdLoja";

        return await conexao.QueryFirstOrDefaultAsync<IntegracaoAds>(sql, new {@IdLoja = Idloja});        
    }

    public async Task<GoogleAdsCredentials?> ObterIntegracaoGoogleAds(string IdLoja)
    {
        using IDbConnection conexao = new FbConnection(_connectionString);

        string sql = @"
            select 
                uuid as IdLoja,
                google_ads_id as GoogleAdsId,
                google_refresh_token as GoogleRefreshToken,
                google_analytics_id as GoogleAnalyticsId
            from loja where uuid = @IdLoja";

        return await conexao.QueryFirstOrDefaultAsync<GoogleAdsCredentials>(sql, new {@IdLoja = IdLoja});       
    }
}

/*
 Observações sobre o repositório e mapeamento de colunas:
 - Este repositório usa Dapper para mapear resultados SQL para POCOs C# (ex.: `IntegracaoAds`, `GoogleAdsCredentials`).
 - Para o mapeamento funcionar corretamente, os aliases SQL devem corresponder exatamente aos nomes das propriedades C# (case-insensitive).
 - Se o método retornar `null`, verifique:
    * se `IdLoja` passado existe no banco;
    * se a query retorna linhas no cliente SQL;
    * se os aliases batem com as propriedades do modelo.
 - Recomendação: adicione logs temporários para inspecionar parâmetros e resultado durante debug (não logar tokens em produção).
*/
