# Inkers Gerenciador de Campanhas

Este repositório contém um microserviço ASP.NET Core que faz parte do projeto **Inkers**. Ele expõe uma API para configuração e criação automatizada de campanhas de tráfego pago, integrando Google Ads, Meta Ads e assistentes de IA para suportar automação de anúncios.

## O que este microserviço faz

- expõe endpoints REST para sincronizar campanhas de tráfego pago
- lê credenciais e configurações de integração a partir do banco de dados Firebird e de variáveis de configuração
- usa `HttpClient` para chamar APIs externas
- usa uma camada de IA para suportar criação/automações de campanhas
- faz parte do módulo de **configuração e criação de campanhas automatizadas** dentro do ecossistema Inkers

## Tecnologias principais

- .NET 10 (`net10.0`)
- ASP.NET Core Web API
- Dapper para acesso aos dados
- Firebird como banco de dados de integração
- Google Ads SDK (`Google.Ads.GoogleAds`)
- OpenAPI / Swagger com `Microsoft.AspNetCore.OpenApi`

## Como configurar 

- Use variáveis de ambiente ou o `dotnet user-secrets` para armazenar dados sensíveis localmente.
- O `.gitignore` do projeto já ignora arquivos de configuração locais e pastas de build.

### 2. Configurações necessárias

O serviço precisa de pelo menos:

- `ConnectionStrings:FirebirdConnection`
- `GoogleAds:DeveloperToken`
- `GoogleAds:ClientId`
- `GoogleAds:ClientSecret`
- `GoogleAds:RefreshToken`
- `GoogleAds:LoginCustomerId`
- `Gemini:ApiKey`
- `SECRET_KEY` (para serviços de criptografia se utilizados)

### 3. Usando `dotnet user-secrets`

No diretório do projeto principal:

```bash
cd /home/bruno/Documentos/inkers_gerenciador_campanhas/inkers_gerencia_campanhas

dotnet user-secrets init

dotnet user-secrets set "ConnectionStrings:FirebirdConnection" "User=YOUR_USER;Password=YOUR_PASS;Database=YOUR_ALIAS;DataSource=127.0.0.1;Port=3050;Dialect=3;Charset=UTF8;ServerType=0;"

dotnet user-secrets set "GoogleAds:DeveloperToken" "YOUR_GOOGLE_ADS_DEVELOPER_TOKEN"

dotnet user-secrets set "GoogleAds:ClientId" "YOUR_GOOGLE_CLIENT_ID"

dotnet user-secrets set "GoogleAds:ClientSecret" "YOUR_GOOGLE_CLIENT_SECRET"

dotnet user-secrets set "GoogleAds:RefreshToken" "YOUR_GOOGLE_REFRESH_TOKEN"

dotnet user-secrets set "GoogleAds:LoginCustomerId" "YOUR_GOOGLE_LOGIN_CUSTOMER_ID"

dotnet user-secrets set "Gemini:ApiKey" "YOUR_GEMINI_API_KEY"

dotnet user-secrets set "SECRET_KEY" "YOUR_SECRET_KEY"
```

> Troque `YOUR_*` pelos valores reais.

### 4. Executar o serviço

```bash
dotnet run --project inkers_gerencia_campanhas/inkers_gerencia_campanhas.csproj
```

Por padrão o serviço roda na URL definida em `appsettings` ou `appsettings.Development.json`.

## Endpoints principais

Os endpoints são registrados em rotas do projeto e podem incluir, por exemplo:

- `GET /api/google/sync/{idloja}` — sincroniza dados de Google Ads para a loja especificada.
- `POST /api/bi/meta/sync` — sincroniza dados de Meta Ads (dependendo de implementação).

> Confira os arquivos em `routes/` para ver todas as rotas registradas.

## Boas práticas

- Não deixe credenciais no repositório.
- Use `appsettings.Development.json` apenas como modelo ou substitua por `appsettings.Development.example.json`.
- Use chaves secretas e conexões em variáveis de ambiente no servidor de produção.
- Faça logs apenas do necessário e nunca logue tokens, senhas ou refresh tokens em texto claro.

## Onde procurar código relevante

- `Program.cs` — inicialização da API e registro de serviços
- `routes/` — definição dos endpoints
- `services/Google/GoogleAdsService.cs` — integração Google Ads
- `services/Criptografia/criptografiaService.cs` — tratamento de criptografia
- `services/Firebird/FirebirdRepository.cs` — consultas ao banco Firebird

---

Este microserviço faz parte do projeto Inkers e foi criado para automatizar a configuração de campanhas de tráfego pago com suporte de assistente de IA, mantendo as credenciais fora do código e usando práticas seguras de configuração.