# Fluxo do microserviço — Inkers Gerenciador de Campanhas

Este documento descreve o fluxo interno deste microserviço: como as requisições chegam, que componentes são invocados e como as integrações externas (Google Ads, Meta, IA) são realizadas.

## Visão geral

O microserviço expõe endpoints HTTP (definidos em `routes/`) que delegam trabalho a serviços em `services/`. Dados de integração e credenciais são lidos via `services/Firebird/FirebirdRepository.cs` (banco Firebird). Tokens sensíveis podem ser descriptografados via `services/Criptografia/criptografiaService.cs`. Chamadas a APIs externas são feitas com `HttpClient` (preferencialmente via `IHttpClientFactory`). A geração de estratégias com IA é feita pelo `GeminiService` (ou serviço de IA configurado).

Arquivos-chave:

- `Program.cs` — inicialização, DI e registro de rotas
- `routes/GoogleRoutes.cs`, `routes/metaRoutes.cs`, `routes/Ai/*` — endpoints públicos
- `services/Google/GoogleAdsService.cs` — lógica de integração Google Ads
- `services/Meta/MetaAdsService.cs` — lógica de integração Meta Ads
- `services/Criptografia/criptografiaService.cs` — descriptografia de tokens
- `services/Firebird/FirebirdRepository.cs` — consultas ao banco Firebird
- `services/Ai/GeminiService.cs` — integração com AI para gerar estratégias

---

## Fluxo passo-a-passo (ex.: chamada `/api/google/sync/{idloja}`)

1. Cliente faz requisição HTTP:
   - Ex.: `GET /api/google/sync/{idloja}`
2. Roteador (route extension) recebe a chamada:
   - Arquivo: `routes/GoogleRoutes.cs`
   - O handler valida parâmetros e aceita DI de serviços (ex.: `GoogleAdsService`).
3. Handler invoca `GoogleAdsService.ProcessarCampanha(IdLoja)`:
   - O serviço foi registrado via `builder.Services.AddHttpClient<GoogleAdsService>();` e outras dependências via DI (ex.: `FirebirdRepository`).
4. `GoogleAdsService` consulta `FirebirdRepository.ObterIntegracaoGoogleAds(IdLoja)`:
   - Recupera credenciais/ids da loja (ex.: `GoogleAdsId`, `GoogleRefreshToken`).
5. Se o token estiver criptografado, passa por `CriptografiaService` para descriptografar.
6. Gera ou troca refreshToken por accessToken (fluxo OAuth) via `GerarAccessToken`:
   - Faz `POST` para `https://oauth2.googleapis.com/token` com `client_id` / `client_secret` / `refresh_token`.
7. Monta `HttpRequestMessage` para a API Google Ads, define headers:
   - `Authorization: Bearer <access_token>`
   - `developer-token: <DeveloperToken>`
8. Envia requisição para Google Ads e processa a resposta:
   - Lê stream/JSON, trata erros, converte resultados para modelos internos.
9. Persiste/retorna resultados conforme necessário e responde ao cliente (ex.: `Results.Ok(...)`).

Fluxos análogos valem para Meta Ads (`MetaAdsService`) — obter token via repositório, descriptografar, montar requisição e enviar.

Fluxo IA (`/api/campanha/ia/gerar-estrategia`):

1. Requisição POST com body `AiCampaignRequest` chega ao route `routes/Ai/AiRoutes.cs`.
2. Validação básica do request (ex.: `LojaId` obrigatório, orçamento mínimo).
3. `GeminiService.GerarEstrategiaCampanha(...)` é chamado com parâmetros do request.
4. `GeminiService` usa o `ApiKey` configurado (via user-secrets/variáveis) e faz chamada HTTP ao endpoint de IA.
5. Resultado (texto/JSON) é retornado ao route e entregue ao cliente.

---

## Diagrama de sequência (texto)

Cliente -> Route Handler -> Service (Google/Meta/Ai)
Service -> FirebirdRepository : ObterIntegracao...
FirebirdRepository -> Banco Firebird : SELECT ...
FirebirdRepository -> Service : Integracao + tokens
Service -> CriptografiaService : Descriptografar(token)
Service -> Auth Endpoint (Google) : POST /token (refresh)
Auth Endpoint -> Service : access_token
Service -> External API (Google/Meta) : Request (Bearer access_token)
External API -> Service : response
Service -> Route Handler : resultado
Route Handler -> Cliente : HTTP response

---

## Modelos de dados importantes

- `GoogleAdsCredentials` (em `models/GoogleIntegracaoAds.cs`)
  - `IdLoja`, `GoogleAdsId`, `GoogleRefreshToken`, `GoogleAnalyticsId`

- `IntegracaoAds` (Meta) — campos equivalentes para Meta tokens

- `AiCampaignRequest` (em `routes/Ai`) — campos usados pelo endpoint IA (LojaId, BioArtista, Estilo, Cidade, OrcamentoMaximo)

---

## Padrões implementados e recomendações

- Dependency Injection (construtor) — já usado para serviços e repositórios.
- Preferir `IHttpClientFactory` (o `AddHttpClient<T>()` está registrado).
- Evitar modificar `HttpClient.DefaultRequestHeaders` em instâncias compartilhadas: em vez disso, use `HttpRequestMessage` e defina headers por requisição.
- Use `CancellationToken` em métodos públicos assíncronos para permitir cancelamento seguro.
- Trate `HttpResponseMessage.IsSuccessStatusCode` e retorne erros compreensíveis (não só `Console.WriteLine`).
- Adicionar `ILogger<T>` a serviços para logs estruturados, em vez de `Console.WriteLine`.

---

## Erros comuns e como depurar

- Resultado nulo ao mapear consultas Dapper: verifique aliases SQL para bater com propriedades C# (ex.: `as GoogleAdsId`).
- "Token inválido" / falha OAuth: logue o `TokenResponse.Content` (temporariamente) para inspecionar a resposta do OAuth.
- Rotas não registradas: confirme que `Program.cs` chama os métodos de extensão de rota (ex.: `app.GoogleRoutesMap();`).

---

## Testes rápidos (curl)

- Testar rota Google:

```bash
curl -v http://localhost:8090/api/google/sync/IDLOJA
```

- Testar rota IA (exemplo):

```bash
curl -X POST http://localhost:8090/api/campanha/ia/gerar-estrategia \
  -H "Content-Type: application/json" \
  -d '{"LojaId":"IDLOJA","BioArtista":"nome","Estilo":"pop","Cidade":"Sao Paulo","OrcamentoMaximo":100}'
```

Substitua `IDLOJA` pelo UUID existente no banco.

---

## Segurança & configuração

- NUNCA comitar `appsettings.Development.json` com valores reais. Use `dotnet user-secrets` ou variáveis de ambiente.
- Não logar tokens/refresh tokens em produção.
- Em produção, use um vault (Azure Key Vault, AWS Secrets Manager) e role-based access.

---

## Próximos passos sugeridos

- Adicionar testes de integração (end-to-end) que simulam respostas do Google/Meta (mock HTTP).
- Cobertura de erros e retry/backoff para chamadas externas.
- Documentar formatos de payloads (ex.: `AiCampaignRequest`, responses) usando OpenAPI/Swagger (já incluído no projecto).

---

Referências: ver arquivos em `routes/`, `services/` e `models/` para detalhes de implementação.
