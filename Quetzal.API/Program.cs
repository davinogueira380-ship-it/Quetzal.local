
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Quetzal.Application.Mapeamentos;
using Quetzal.Application.Servicos.Implementacoes;
using Quetzal.Application.Servicos.Interfaces;
using Quetzal.Infrastructure;
using Quetzal.Infrastructure.Dados;
using Quetzal.Application.Servicos;

var builder = WebApplication.CreateBuilder(args);

// ================================================================
// INFRASTRUCTURE
// ================================================================

builder.Services.AdicionarServicosDeInfraestrutura(
    builder.Configuration);

builder.Services.AddDataProtection();

// ================================================================
// AUTENTICAÇÃO JWT DA API
// ================================================================

var jwtChave = builder.Configuration["Jwt:Chave"]
    ?? throw new InvalidOperationException("Jwt:Chave não foi configurada.");

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = builder.Configuration["Jwt:Emissor"],
        ValidAudience = builder.Configuration["Jwt:Audiencia"],
        IssuerSigningKey = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(jwtChave)),
        RoleClaimType = ClaimTypes.Role,
        NameClaimType = ClaimTypes.Name,
        ClockSkew = TimeSpan.Zero
    };
});

// ================================================================
// CONTROLLERS
// ================================================================

builder.Services.AddControllers();

builder.Services.AddScoped<IUsuarioServico, UsuarioServico>();

// ================================================================
// SWAGGER
// ================================================================

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "Quetzal API",
        Version = "v1",
        Description = "API REST do sistema Quetzal — Catálogo de Games para ensino de ASP.NET Core"
    });
});
// =====================================================================
// 6. CORS — Permite requisições de outras origens
// =====================================================================
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});
// ================================================================
// AUTOMAPPER
// ================================================================

builder.Services.AddAutoMapper(
    cfg => { },
    typeof(PerfilMapeamento));

// ================================================================
// AUTHORIZATION
// ================================================================

builder.Services.AddAuthorization();

// ================================================================
// SERVIÇOS DA APPLICATION
// ================================================================

builder.Services.AddScoped<IAmbienteServico, AmbienteServico>();
builder.Services.AddScoped<IPortfolioServico, PortfolioServico>();
builder.Services.AddScoped<IProjetoCServico, ProjetoCServico>();

// ================================================================
// CONSTRUÇÃO DA APLICAÇÃO
// ================================================================

var app = builder.Build();

// ================================================================
// SWAGGER
// ================================================================

app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Quetzal API V1");
    c.RoutePrefix = string.Empty; // Define o Swagger como a página inicial
});

// ================================================================
// HTTPS
// ================================================================

//app.UseHttpsRedirection();

// ================================================================
// AUTENTICAÇÃO E AUTORIZAÇÃO
// ================================================================

app.UseAuthentication();
app.UseAuthorization();

// ================================================================
// CONTROLLERS
// ================================================================

app.MapControllers();

// ================================================================
// BANCO DE DADOS E SEED
// ================================================================

using (var scope = app.Services.CreateScope())
{
    await SeedDados.InicializarAsync(scope.ServiceProvider);
}

// ================================================================
// EXECUÇÃO DA APLICAÇÃO
// ================================================================

app.Run();

