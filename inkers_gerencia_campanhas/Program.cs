using Inkers.GerenciadorCampanhas.Routes;
using Inkers.GerenciadorCampanhas.Routes.Google;
using Inkers.GerenciadorCampanhas.Services.Google;
using Inkers.GerenciadorCampanhas.Services.Meta;    
using Inkers.GerenciadorCampanhas.Services.Firebird;
using Inkers.GerenciadorCampanhas.Services.Criptografia;
using Inkers.GerenciadorCampanhas.Services.ai.Gemini;
using Inkers.GerenciadorCampanhas.Routes.Ai.AiRoutes;


var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddHttpClient<MetaAdsService>();
builder.Services.AddHttpClient<GoogleAdsService>();
builder.Services.AddHttpClient<GeminiService>();
builder.Services.AddScoped<FirebirdRepository>();
builder.Services.AddScoped<CriptografiaService>();

var app = builder.Build();

app.UseHttpsRedirection();

app.MapMetaRoutes();
app.GoogleRoutesMap();
app.MapCampanhaIaRoutes();


app.Run("http://localhost:8090");

