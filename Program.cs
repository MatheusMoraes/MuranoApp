using System.Text;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using MuranoApp;
using MuranoApp.Controllers;
using MuranoApp.Data;
using MuranoApp.Options;
using MuranoApp.Services;
using Npgsql;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddSingleton<IApiBlockStateStore, InMemoryApiBlockStateStore>();

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

// O Supabase (pooler Supavisor) e/ou a rede entre o Render e o banco podem
// derrubar conexões ociosas em silêncio (sem RST/FIN), fazendo o Npgsql
// entregar do seu pool interno uma conexão "morta": o app só descobre isso
// ao tentar usá-la, travando até o CommandTimeout. Keepalive + reciclagem
// mais agressiva do pool reduzem a chance disso acontecer; CommandTimeout
// menor reduz o custo quando ainda assim acontecer (as queries reais aqui
// levam ~20ms, então 30s de espera era desproporcional).
var npgsqlConnectionStringBuilder = new NpgsqlConnectionStringBuilder(
    builder.Configuration.GetConnectionString("DefaultConnection"))
{
    TcpKeepAlive = true,
    TcpKeepAliveTime = 30,
    TcpKeepAliveInterval = 10,
    Timeout = 15,
    ConnectionIdleLifetime = 60,
    ConnectionPruningInterval = 10
};

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(
        npgsqlConnectionStringBuilder.ConnectionString,
        npgsqlOptions => npgsqlOptions
            .EnableRetryOnFailure(maxRetryCount: 3, maxRetryDelay: TimeSpan.FromSeconds(5), errorCodesToAdd: null)
            .CommandTimeout(15)));

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// CORS travado nos domínios reais do front (não dá pra usar AllowAnyOrigin
// junto com AllowCredentials, que agora é necessário para o cookie de
// autenticação ir/voltar nas chamadas). "AllowedOrigins" vem de config
// (appsettings/env var) para não precisar recompilar ao trocar domínio.
var allowedOrigins = builder.Configuration
    .GetSection("AllowedOrigins")
    .Get<string[]>() ?? new[]
    {
        "https://murano-front-livid.vercel.app",
        "http://localhost:5173"
    };

builder.Services.AddCors(options =>
{
    options.AddPolicy("Frontend",
        policy =>
        {
            policy
                .WithOrigins(allowedOrigins)
                .AllowAnyMethod()
                .AllowAnyHeader()
                .AllowCredentials();
        });
});

// Autenticação: JWT emitido no Login e devolvido como cookie httpOnly (o
// front nunca lê nem guarda o token). Como o token chega via cookie e não
// via header Authorization, precisamos ensinar o middleware a procurá-lo
// no cookie (OnMessageReceived).
var jwtKey = builder.Configuration["Jwt:Key"]
    ?? throw new InvalidOperationException("Jwt:Key não configurado.");

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidateAudience = true,
            ValidAudience = builder.Configuration["Jwt:Audience"],
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromMinutes(1)
        };

        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                if (context.Request.Cookies.TryGetValue("murano_auth", out var token))
                {
                    context.Token = token;
                }
                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorization();

// Rate limit no login: alvo natural de força bruta por ser a única rota
// anônima que valida credenciais. Janela fixa por IP — 5 tentativas por
// minuto é suficiente pro uso normal (o usuário raramente erra a senha mais
// de uma ou duas vezes) e já dificulta bastante um brute-force.
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    options.AddPolicy("login", context =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 5,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0
            }));
});

var app = builder.Build();

app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseCors("Frontend");
app.UseMiddleware<RequestTimingMiddleware>();

app.MapOpenApi();
app.UseSwagger();
app.UseSwaggerUI();

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();

app.MapControllers();

app.Run();
