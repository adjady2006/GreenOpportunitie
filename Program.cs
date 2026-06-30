using System.Text;
using System.Text.Json.Serialization;
using GreenOpportunities.API.Data;
using GreenOpportunities.API.Helpers;
using GreenOpportunities.API.Repositories;
using GreenOpportunities.API.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// =========================
// 1. Configuration DbContext (SQLite)
// =========================
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
                       ?? "Data Source=greenopportunities.db";

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(connectionString)
           .ConfigureWarnings(w => w.Ignore(
               Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning)));

// =========================
// 2. JWT Settings
// =========================
var jwtSettings = builder.Configuration.GetSection("Jwt").Get<JwtSettings>()
                  ?? throw new InvalidOperationException("Section 'Jwt' manquante dans la configuration.");

if (string.IsNullOrWhiteSpace(jwtSettings.Secret) || jwtSettings.Secret.Length < 32)
{
    throw new InvalidOperationException(
        "La clé JWT doit être définie (clé >= 32 caractères). Vérifiez appsettings.json / variables d'environnement.");
}

builder.Services.AddSingleton(jwtSettings);
builder.Services.AddSingleton<JwtTokenGenerator>();

// =========================
// 3. Authentification JWT
// =========================
builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtSettings.Issuer,
            ValidAudience = jwtSettings.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.Secret)),
            ClockSkew = TimeSpan.FromMinutes(2),
        };
    });

builder.Services.AddAuthorization();

// =========================
// 4. Injection des Repositories / Services
// =========================
builder.Services.AddScoped<IOpportunityRepository, OpportunityRepository>();
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IOpportunityService, OpportunityService>();
builder.Services.AddScoped<IAuthService, AuthService>();

// =========================
// 5. Controllers + JSON (évite les cycles sur les références)
// =========================
builder.Services
    .AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
        options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
    });

// Personnalisation des réponses d'erreur de validation
builder.Services.Configure<ApiBehaviorOptions>(options =>
{
    options.InvalidModelStateResponseFactory = context =>
    {
        var problemDetails = new ValidationProblemDetails(context.ModelState)
        {
            Title = "Erreur de validation",
            Status = StatusCodes.Status400BadRequest,
        };
        return new BadRequestObjectResult(problemDetails)
        {
            ContentTypes = { "application/json" },
        };
    };
});

// =========================
// 6. Swagger / OpenAPI (avec support Bearer Token)
// =========================
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Green Opportunities API",
        Version = "v1",
        Description = "API RESTful pour la gestion des opportunités (فرص خضراء) – ASP.NET Core Web API.",
        Contact = new OpenApiContact
        {
            Name = "Green Opportunities",
            Email = "info@foraskhadra.com",
            Url = new Uri("https://www.foraskhadra.com"),
        },
    });

    var securityScheme = new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Description = "Saisir le token JWT (sans le préfixe 'Bearer ').",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        Reference = new OpenApiReference
        {
            Id = JwtBearerDefaults.AuthenticationScheme,
            Type = ReferenceType.SecurityScheme,
        },
    };
    c.AddSecurityDefinition(securityScheme.Reference.Id, securityScheme);
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        { securityScheme, Array.Empty<string>() },
    });
});

var app = builder.Build();

// =========================
// 7. Seed automatique (création des tables + données démo)
// =========================
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();

    // Vérifier si la table Opportunities est vide pour injecter des données démo
    if (!db.Opportunities.Any())
    {
        db.Opportunities.AddRange(
            new GreenOpportunities.API.Models.Opportunity
            {
                Title = "Bourse Chevening au Royaume-Uni",
                Description = "Bourse entièrement financée du gouvernement britannique pour un master d'un an au Royaume-Uni.",
                Type = "Scholarship",
                Country = "United Kingdom",
                Deadline = DateTime.UtcNow.AddMonths(3),
                IsFullyFunded = true,
                CreatedAt = DateTime.UtcNow,
            },
            new GreenOpportunities.API.Models.Opportunity
            {
                Title = "Formation en énergie solaire - Maroc",
                Description = "Programme de formation pratique sur les installations photovoltaïques destiné aux jeunes ingénieurs marocains.",
                Type = "Training",
                Country = "Morocco",
                Deadline = DateTime.UtcNow.AddMonths(2),
                IsFullyFunded = true,
                CreatedAt = DateTime.UtcNow,
            },
            new GreenOpportunities.API.Models.Opportunity
            {
                Title = "Concours Hult Prize - Édition mondiale",
                Description = "Concours mondial d'entrepreneuriat social axé sur les Objectifs de Développement Durable.",
                Type = "Competition",
                Country = "Global",
                Deadline = DateTime.UtcNow.AddMonths(4),
                IsFullyFunded = false,
                CreatedAt = DateTime.UtcNow,
            },
            new GreenOpportunities.API.Models.Opportunity
            {
                Title = "Bourse DAAD EPOS – Master en Allemagne",
                Description = "Programme de bourses du DAAD pour des masters liés au développement durable dans les universités allemandes.",
                Type = "Scholarship",
                Country = "Germany",
                Deadline = DateTime.UtcNow.AddMonths(5),
                IsFullyFunded = true,
                CreatedAt = DateTime.UtcNow,
            },
            new GreenOpportunities.API.Models.Opportunity
            {
                Title = "Stage UNICEF – Plaidoyer climatique",
                Description = "Stage de 6 mois au sein du bureau UNICEF dédié à la justice climatique pour les jeunes.",
                Type = "Internship",
                Country = "Switzerland",
                Deadline = DateTime.UtcNow.AddMonths(1),
                IsFullyFunded = false,
                CreatedAt = DateTime.UtcNow,
            }
        );
        db.SaveChanges();
    }

    // Vérifier si l'utilisateur admin par défaut existe, sinon le créer
    if (!db.Users.Any(u => u.Username == "admin"))
    {
        db.Users.Add(new GreenOpportunities.API.Models.ApplicationUser
        {
            Username = "admin",
            Email = "admin@foraskhadra.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin@123"),
            Role = "Admin",
            CreatedAt = DateTime.UtcNow,
        });
        db.SaveChanges();
    }
}

// =========================
// 8. Pipeline HTTP
// =========================
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Green Opportunities API v1");
    c.RoutePrefix = "swagger";
});

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.MapGet("/", () => Results.Redirect("/swagger"));

app.Run();

/// <summary>
/// Classe partielle utilisée pour permettre l'accès à Program dans les tests (par ex. WebApplicationFactory).
/// </summary>
public partial class Program { }