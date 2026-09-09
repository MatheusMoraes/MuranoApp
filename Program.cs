using Microsoft.EntityFrameworkCore;
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

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll",
        policy =>
        {
            policy
                .AllowAnyOrigin()
                .AllowAnyMethod()
                .AllowAnyHeader();
        });
});

var app = builder.Build();

app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseCors("AllowAll");
app.UseMiddleware<RequestTimingMiddleware>();

app.MapOpenApi();
app.UseSwagger();
app.UseSwaggerUI();

app.UseHttpsRedirection();

app.MapControllers();

app.Run();
