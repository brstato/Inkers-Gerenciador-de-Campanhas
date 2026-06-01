public class GoogleIntegracaoAds
{
    public long Id {get; set;}
    public required string Nome{get; set;}
    public long Cliques{get; set;}
    public double CustoTotal{get; set;}
}

public class GoogleAdsCredentials
{
    public required string IdLoja {get; set;}
    public required string GoogleAdsId {get; set;}
    public required string GoogleRefreshToken {get; set;}
    public required string GoogleAnalyticsId {get; set;}
}
