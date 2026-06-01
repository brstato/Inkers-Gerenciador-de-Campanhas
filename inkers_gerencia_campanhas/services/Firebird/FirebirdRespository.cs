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
