using Inkers.GerenciadorCampanhas.Routes;
using Inkers.GerenciadorCampanhas.Routes.Google;
using Inkers.GerenciadorCampanhas.Services.Google;
using Inkers.GerenciadorCampanhas.Services.Meta;    
using Inkers.GerenciadorCampanhas.Services.Firebird;
using Inkers.GerenciadorCampanhas.Services.Criptografia;
using Inkers.GerenciadorCampanhas.Services.Ai;
using Inkers.GerenciadorCampanhas.Routes.Ai.AiRoutes;
using Inkers.GerenciadorCampanhas.Services.Google.GoogleAdsBuilderService;


var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddHttpClient<MetaAdsService>();
builder.Services.AddHttpClient<GoogleAdsService>();
builder.Services.AddHttpClient<GeminiService>();
builder.Services.AddScoped<FirebirdRepository>();
builder.Services.AddScoped<CriptografiaService>();
builder.Services.AddScoped<GoogleAdsBuilderService>();

var app = builder.Build();

app.UseHttpsRedirection();

app.MapMetaRoutes();
app.GoogleRoutesMap();
app.MapCampanhaIaRoutes();


app.Run("http://localhost:8092");

