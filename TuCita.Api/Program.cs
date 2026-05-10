using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using System.Text;
using TuCita.Api.Authorization;
using TuCita.Api.BackgroundJobs;
using TuCita.Api.Storage;
using TuCita.Infrastucture;
using TuCita.Infrastucture.Persistence;

var builder = WebApplication.CreateBuilder(args);

// Controllers
builder.Services.AddControllers();
builder.Services.AddScoped<IFileStorageService, LocalFileStorageService>();

// Infrastructure
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.Configure<BackgroundJobsOptions>(
    builder.Configuration.GetSection(BackgroundJobsOptions.SectionName));
builder.Services.AddHostedService<TuCitaBackgroundWorker>();

// Swagger
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(options =>
{
    const string securitySchemeName = "Bearer";

    options.AddSecurityDefinition(securitySchemeName, new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = JwtBearerDefaults.AuthenticationScheme,
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Ingrese el token JWT. Ejemplo: Bearer {token}"
    });

    options.AddSecurityRequirement(document => new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecuritySchemeReference(securitySchemeName, document),
            new List<string>()
        }
    });
});

// JWT
var jwtKey = builder.Configuration["Jwt:Key"]
    ?? throw new InvalidOperationException("Jwt:Key no está configurado.");

var jwtIssuer = builder.Configuration["Jwt:Issuer"]
    ?? throw new InvalidOperationException("Jwt:Issuer no está configurado.");

var jwtAudience = builder.Configuration["Jwt:Audience"]
    ?? throw new InvalidOperationException("Jwt:Audience no está configurado.");

builder.Services
    .AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.RequireHttpsMetadata = !builder.Environment.IsDevelopment();
        options.SaveToken = true;

        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateIssuerSigningKey = true,
            ValidateLifetime = true,

            ValidIssuer = jwtIssuer,
            ValidAudience = jwtAudience,
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(jwtKey)),

            ClockSkew = TimeSpan.FromMinutes(2)
        };
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy(TuCitaPolicies.BusinessOwnerAdmin, policy =>
    {
        policy.RequireAuthenticatedUser();
        policy.AddRequirements(new BusinessAccessRequirement("Owner", "Admin"));
    });

    options.AddPolicy(TuCitaPolicies.BusinessAgenda, policy =>
    {
        policy.RequireAuthenticatedUser();
        policy.AddRequirements(new BusinessAccessRequirement("Owner", "Admin", "Recepcionista"));
    });

    options.AddPolicy(TuCitaPolicies.BusinessProfessional, policy =>
    {
        policy.RequireAuthenticatedUser();
        policy.AddRequirements(new BusinessAccessRequirement("Owner", "Admin", "Recepcionista", "Profesional"));
    });
});
builder.Services.AddScoped<IAuthorizationHandler, BusinessAccessHandler>();

// CORS
var origenesPermitidos = builder.Configuration
    .GetValue<string>("origenesPermitidos")?
    .Split(",", StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
    ?? [];

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy
            .WithOrigins(origenesPermitidos)
            .AllowAnyMethod()
            .AllowAnyHeader();
    });
});

var app = builder.Build();

await DatabaseSeeder.SeedAsync(app.Services);

// Pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseStaticFiles();

app.UseCors();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
