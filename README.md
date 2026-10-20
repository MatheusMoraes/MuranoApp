# Mistério de Murano — Backend

API em .NET 9 / EF Core / PostgreSQL (Neon) para o sistema de estoque e pedidos da loja Mistério de Murano.

Este documento cobre as demandas mais recentes: **dashboard, índice de nome, filtro de pedidos, rate limiting, testes automatizados e multiusuário com dono de cadastro**. Para o restante do sistema (autenticação, produtos, categorias, upload de imagens, etc.), veja o código-fonte.

## Sumário

1. [Índice de nome (Produto/Categoria)](#1-índice-de-nome-produtocategoria)
2. [Dashboard](#2-dashboard)
3. [Filtro e listagem de Pedidos](#3-filtro-e-listagem-de-pedidos)
4. [Rate limiting](#4-rate-limiting)
5. [Testes automatizados](#5-testes-automatizados)
6. [Multiusuário e dono de cadastro](#6-multiusuário-e-dono-de-cadastro)

---

## 1. Índice de nome (Produto/Categoria)

**Problema:** a checagem de nome duplicado de Produto e Categoria ("batata quente" == "BaTaTa QUENTE") carregava a tabela inteira para a memória a cada criação/edição e comparava registro por registro em C# — um full table scan a cada chamada.

**Solução:**
- Nova coluna `NomeNormalizado` em `Product` e `Category` (trim + colapso de espaços + minúsculo), preenchida pela aplicação em toda criação/edição via `Services/NameNormalizer.cs` (normalização compartilhada entre as duas entidades).
- Índice único do Postgres sobre `NomeNormalizado` em ambas as tabelas (`Data/AppDbContext.cs`).
- A checagem de duplicidade virou uma busca indexada (`AnyAsync(p => p.NomeNormalizado == normalizado)`) em vez de carregar a tabela inteira.
- Rede de segurança contra corrida: se duas requisições passarem pela checagem em memória ao mesmo tempo, o índice único do Postgres rejeita a segunda gravação (`Npgsql.PostgresException` com `SqlState == "23505"`), convertida numa `ArgumentException` amigável em `TrySaveOrThrowDuplicateAsync`.
- Migration `AddNameNormalizedIndex`: adiciona a coluna, faz o backfill dos registros já existentes via SQL (`lower(regexp_replace(btrim(...), '\s+', ' ', 'g'))`) e só então cria o índice único — mesmo padrão de migração segura usado em `AddProductCategory`/`AllowProductDeletionWithOrders`.

**Arquivos:** `Services/NameNormalizer.cs`, `Models/Product.cs`, `Models/Category.cs`, `Data/AppDbContext.cs`, `Services/ProductsService.cs`, `Services/CategoryService.cs`, `Migrations/20260912015245_AddNameNormalizedIndex.cs`.

## 2. Dashboard

Novo endpoint `GET /api/dashboard` (autenticado), com serviço `DashboardService` calculando tudo sob demanda a partir das tabelas existentes (sem tabela própria de agregação — o volume de dados de uma loja pequena não justifica isso).

**Métricas retornadas:**
- **KPIs gerais:** total de pedidos, receita total, ticket médio, total de clientes cadastrados, total de produtos cadastrados, produtos com estoque baixo.
- **Top 5 clientes** por número de pedidos (desempate por valor total gasto).
- **Top 5 produtos mais vendidos** por quantidade — agrupado pelo `NomeProduto` (snapshot salvo em cada `OrderItem`) em vez do `ProdutoId`, para continuar correto mesmo depois que um produto é excluído do cadastro.
- **Receita por categoria** (melhor esforço): itens cujo produto já foi excluído não têm mais categoria conhecida e ficam de fora só dessa métrica específica.

A ordenação final dos rankings é feita em memória (depois de trazer os dados agregados do banco) em vez de via `ORDER BY` no SQL — o SQLite (usado nos testes automatizados) não traduz ordenação sobre coluna `decimal`, e como o volume de pedidos de uma loja pequena é baixo, não há custo real em ordenar client-side. O ganho é rodar de forma idêntica em qualquer provider.

No front-end, `pages/DashboardPage.tsx` (rota `/`, padrão após login, link no menu) exibe os KPIs como *stat tiles* e os rankings como barras horizontais de um único tom (dourado da marca), com o valor de cada barra sempre exibido como texto ao lado.

**Arquivos:** `DTOs/DashboardResponseDTO.cs`, `Services/DashboardService.cs`, `Controllers/DashboardController.cs` (backend); `src/types/dashboard.ts`, `src/pages/DashboardPage.tsx`, `src/App.tsx`, `src/index.css` (front-end).

### 2.1 Receita por período

Card dedicado no dashboard com o histórico de receita ao longo do tempo, filtrável por `GET /api/dashboard/revenue?period=<chave>`.

- **Períodos aceitos:** `30d` (padrão), `60d`, `90d`, `trimestre` (3 meses), `semestre` (6 meses), `ano` (12 meses).
- **Granularidade automática por período** (`DashboardService.Periodos`): 30/60/90 dias agrupam por dia; trimestre/semestre por janelas de 7 dias; ano por mês calendário — evita um gráfico com 365 pontos diários ilegível, sem exigir nada do front além de trocar o filtro.
- Buckets sem pedido entram com receita 0 (não pulam a data), mantendo o eixo X contínuo.
- Agrupamento feito em memória (não via `GROUP BY` no SQL) pelo mesmo motivo dos rankings acima: portabilidade entre Postgres (produção) e SQLite (testes), com custo desprezível dado o volume de uma loja pequena.
- No front-end, `src/components/RevenueByPeriodCard.tsx` renderiza um gráfico de linha/área em SVG puro (sem biblioteca de gráficos), com filtro em pílulas, crosshair + tooltip no hover (mouse e teclado, setas ← →), marcador no último ponto e alternância para visualizar os mesmos dados como tabela — sem depender só de cor pra transmitir a informação.

**Arquivos:** `DTOs/RevenueByPeriodDTO.cs`, `Services/DashboardService.cs` (`GetRevenueByPeriodAsync`), `Controllers/DashboardController.cs` (backend); `src/types/dashboard.ts`, `src/components/RevenueByPeriodCard.tsx`, `src/pages/DashboardPage.tsx`, `src/index.css` (front-end).

## 3. Filtro e listagem de Pedidos

- Campo de busca na tela de Pedidos (`src/components/OrderList.tsx`), filtrando pelo nome do cliente ou pelo número do pedido (aceita `#12` ou `12`).
- O cabeçalho de cada pedido na listagem passou a exibir também o **nome do cliente** e o **valor total** do pedido, além do número ("Pedido #12 — Nome do Cliente" / "data · R$ valor"), sem precisar abrir os detalhes.

**Arquivos:** `src/components/OrderList.tsx`, `src/index.css`.

## 4. Rate limiting

Rate limit no endpoint de login (`POST /api/Login`), usando o middleware nativo do ASP.NET Core (`Microsoft.AspNetCore.RateLimiting`, incluso no shared framework desde o .NET 7 — nenhum pacote NuGet novo foi necessário).

- Política `"login"`: janela fixa de **5 tentativas por minuto por IP** (`RateLimitPartition.GetFixedWindowLimiter`, chaveado pelo IP remoto da conexão).
- Ao estourar o limite, a API responde `429 Too Many Requests`.
- O login é o único endpoint anônimo que valida credenciais, sendo o alvo natural de tentativas de força bruta — 5/min é suficiente para o uso normal (raramente o usuário erra a senha mais de uma ou duas vezes) e já dificulta bastante um ataque automatizado.

**Arquivos:** `Program.cs`, `Controllers/AuthController.cs`.

## 5. Testes automatizados

Novo projeto `MuranoApp.Tests` (xUnit), referenciando `MuranoApp.csproj` e usando **SQLite em memória** como provider de banco de dados nos testes.

**Por que SQLite e não o provider InMemory do EF Core:** `OrderService` usa APIs só disponíveis em providers relacionais de verdade (`Database.BeginTransactionAsync`, `Database.CreateExecutionStrategy`, necessárias para combinar transação manual com `EnableRetryOnFailure`). O provider InMemory do EF Core lança `InvalidOperationException` nessas chamadas. SQLite em memória é um banco relacional real, leve o suficiente para rodar em cada teste sem infraestrutura externa.

Cada classe de teste abre sua própria conexão SQLite `:memory:` (mantida viva durante o teste via `SqliteContextFixture`, já que o banco `:memory:` do SQLite desaparece quando a conexão fecha) — como o xUnit instancia a classe de teste uma vez por `[Fact]`/`[Theory]`, cada teste roda isolado, sem estado compartilhado entre eles.

**Cobertura (49 testes):**
- `NameNormalizerTests`: normalização de nome (trim, espaços, caixa).
- `ProductServiceTests`: nome normalizado preenchido ao criar; rejeição de nome duplicado (ignorando caixa/espaços); mesmo nome permitido em catálogos de usuários diferentes; permitir manter o próprio nome ao editar; validação de preço de atacado (não pode exceder o varejo, precisa vir com quantidade mínima); categoria inexistente; usuário comum não vê/edita/exclui produto de outro usuário; admin vê/edita qualquer um.
- `CategoryServiceTests`: nome normalizado preenchido ao criar; rejeição de nome duplicado; exclusão bloqueada quando há produtos associados; exclusão permitida quando não há.
- `OrderServiceTests`: decremento de estoque ao criar pedido; resolução de preço varejo vs. atacado (abaixo e no limite da quantidade mínima); erro de estoque insuficiente; erro de cliente/produto inexistente; erro quando cliente não tem endereço cadastrado e nenhum é informado; uso do endereço informado no pedido; restauração de estoque ao excluir pedido; exclusão de pedido não quebra quando o produto do item já foi excluído (`ProdutoId` nulo); ajuste de estoque correto ao editar um pedido existente; usuário comum só vê os próprios pedidos e não exclui pedido de outro usuário; admin vê todos.
- `DashboardServiceTests`: retorno zerado sem pedidos; ranking de clientes por número de pedidos; produto excluído continua contabilizado no ranking via `NomeProduto`; receita por período rejeita período inválido; `30d` agrupa por dia e ignora pedidos fora da janela; `ano` agrupa por mês; usuário comum só vê dados próprios no dashboard e na receita por período; admin vê de todos.

**Rodando os testes:**
```bash
dotnet test
```

**Gap conhecido (documentado, não coberto por teste):** a rede de segurança contra corrida na checagem de nome único (`catch (DbUpdateException ex) when (IsUniqueViolation(ex))`) depende de `Npgsql.PostgresException`, específica do Postgres — não é disparada rodando contra SQLite, então esse caminho fica coberto apenas pela validação em memória (testada) e pela garantia estrutural do índice único em produção.

## 6. Multiusuário e dono de cadastro

A aplicação passou a suportar mais de um usuário logado, com dois papéis:

- **Admin** (`IsAdmin = true`): vê e edita produtos e pedidos de **qualquer** usuário, sem restrição.
- **Usuário comum**: só vê e edita os produtos/pedidos que **ele mesmo** cadastrou.

**Como funciona:**
- `Product` e `Order` ganharam a coluna `UserId` (dono do cadastro, preenchido automaticamente na criação a partir do usuário autenticado — nunca aceito do corpo da requisição). `Client` e `Category` continuam compartilhados entre todos os usuários (não têm dono).
- O JWT passou a carregar uma claim de role (`Admin`/`User`), lida via `ClaimsPrincipal.GetUserId()`/`IsAdminUser()` (`Extensions/ClaimsPrincipalExtensions.cs`) em todo controller que precisa filtrar por dono.
- `ProductService`/`OrderService`: toda consulta (`GetAllAsync`, `GetByIdAsync`) filtra por `UserId` quando o usuário não é admin; `UpdateAsync`/`DeleteAsync` tratam um registro de outro usuário como **inexistente** (404), não como "403 Forbidden" — não revela nem que o registro existe pra quem não tem acesso.
- `DashboardService` (KPIs, rankings, receita por período) também filtra Pedidos/Produtos por `UserId`; `TotalClientes` fica de fora do filtro de propósito, já que `Client` é compartilhado.
- **Nome de produto único por catálogo, não mais global**: o índice único passou de `NomeNormalizado` para `(UserId, NomeNormalizado)` — dois usuários podem ter produtos de mesmo nome em catálogos separados.
- Migration `AddUserOwnership`: adiciona as colunas, faz backfill de todo `Product`/`Order` já existente pro usuário que já estava cadastrado (dados antigos continuam visíveis pra ele), e cria o usuário admin.

**Troca de senha (tela de perfil):** novo endpoint `PUT /api/ChangePassword` (autenticado), exige a senha atual correta antes de trocar — sem confirmação por e-mail (não há provedor configurado); a sessão logada já comprova a identidade. Hash continua PBKDF2 salgado via `PasswordHasher<AdminUser>` do ASP.NET Core, igual ao hash de criação de conta.

No front-end: avatar circular (iniciais do nome) no header, substituindo o botão solto "Sair" — abre um dropdown com nome/e-mail, link pra `/perfil` e "Sair". A tela de perfil (`src/pages/ProfilePage.tsx`) mostra os dados do usuário (com badge "Administrador"/"Usuário") e o formulário de troca de senha.

**Arquivos:** `Models/AdminUser.cs`, `Models/Product.cs`, `Models/Order.cs`, `Data/AppDbContext.cs`, `Extensions/ClaimsPrincipalExtensions.cs`, `Services/AuthService.cs`, `Services/ProductsService.cs`, `Services/OrdersService.cs`, `Services/DashboardService.cs`, `Controllers/AuthController.cs`, `Controllers/ProductsController.cs`, `Controllers/OrdersController.cs`, `Controllers/DashboardController.cs`, `DTOs/ChangePasswordDTO.cs`, `Migrations/20261019220701_AddUserOwnership.cs` (backend); `src/types/auth.ts`, `src/api/auth.ts`, `src/pages/ProfilePage.tsx`, `src/utils/getIniciais.ts`, `src/App.tsx`, `src/index.css` (front-end).

---

*Documentação gerada para as demandas: dashboard, índice de nome, filtro de pedidos, rate limiting, testes automatizados e multiusuário com dono de cadastro.*
