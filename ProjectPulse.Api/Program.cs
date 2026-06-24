using Microsoft.EntityFrameworkCore;
using ProjectPulse.Api.Persistence;
using Microsoft.OpenApi.Models;
using FluentValidation;
using FluentValidation.AspNetCore;
using ProjectPulse.Api.DTOs;
using Microsoft.AspNetCore.Mvc;
using System.Reflection;
using ProjectPulse.Api.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;




var builder = WebApplication.CreateBuilder(args);



builder.Services.AddControllers();
builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddScoped<IPasswordHasher, PasswordHasher>();
// Opciones para JWT leídas de appsettings.Development.json ("Jwt")
builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection("Jwt"));

// Servicios propios
builder.Services.AddScoped<IPasswordHasher, PasswordHasher>();
builder.Services.AddScoped<IJwtTokenService, JwtTokenService>();


// Descubre validadores buscando en el ensamblado del DTO
builder.Services.AddValidatorsFromAssemblyContaining<ProjectCreateDto>();

// Controllers
builder.Services.AddControllers();
builder.Services.AddFluentValidationAutoValidation(); // activa el filtro automático 400
builder.Services.AddValidatorsFromAssemblyContaining<ProjectCreateDtoValidator>(); //validadores


builder.Services.Configure<ApiBehaviorOptions>(o =>
{
    o.InvalidModelStateResponseFactory = ctx =>
    {
        var problem = new ValidationProblemDetails(ctx.ModelState)
        {
            Status = StatusCodes.Status400BadRequest,
            Title = "Validation failed."
        };
        return new BadRequestObjectResult(problem);
    };
});


// Swagger / OpenAPI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "ProjectPulse API", Version = "v1" });
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        In = ParameterLocation.Header,
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        Description = "JWT en el encabezado. Ejemplo: Bearer {token}"
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement {
        { new OpenApiSecurityScheme { Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" } }, Array.Empty<string>() }
    });
});

// Auth: JWT Bearer
var jwt = builder.Configuration.GetSection("Jwt");
var key = Encoding.UTF8.GetBytes(jwt["Key"]!);

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(o =>
    {
        o.TokenValidationParameters = new Microsoft.IdentityModel.Tokens.TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwt["Issuer"],
            ValidAudience = jwt["Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(key)
        };
    });


builder.Services.AddCors(o =>
{
    o.AddPolicy("frontend", p =>
        p.WithOrigins("http://localhost:5173", "http://localhost:3000")
         .AllowAnyHeader()
         .AllowAnyMethod());
});

// --- Connection string robusta con fallback y log ---
var connStr = builder.Configuration.GetConnectionString("Default");

// Fallback si viene vacía (Docker o entorno que no cargó appsettings)
if (string.IsNullOrWhiteSpace(connStr))
{
    // Intenta leer desde variable de entorno explícita
    connStr = Environment.GetEnvironmentVariable("ConnectionStrings__Default");

    if (string.IsNullOrWhiteSpace(connStr))
    {
        // Último recurso: usa SQLite local por defecto
        connStr = "Data Source=projectpulse.db";
    }
}

// Log seguro para ver qué se está usando en contenedor
builder.Logging.AddConsole();
builder.Logging.AddDebug();
builder.Services.AddSingleton(new { ConnectionStringInUse = connStr });

// Registra DbContext con la cadena final
builder.Services.AddDbContext<AppDbContext>(opt => opt.UseSqlite(connStr));
var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/error");   // prod
}
else
{
    app.UseDeveloperExceptionPage();     // dev
}

// Punto central de errores → /error
app.Map("/error", (HttpContext ctx) =>
{
    // ASP.NET generará automáticamente un ProblemDetails 500 aquí
    return Results.Problem(title: "Unexpected error", statusCode: StatusCodes.Status500InternalServerError);
});

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "ProjectPulse API v1");
    });
}

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
    var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("Seeder");
    await db.EnsureSeededAsync(hasher, logger);
}

// Swagger habilitado también si se pasa EnableSwagger=true por entorno
var enableSwagger = app.Environment.IsDevelopment()
                    || builder.Configuration.GetValue<bool>("EnableSwagger");

if (enableSwagger)
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "ProjectPulse API v1");
        c.RoutePrefix = "swagger"; // mantiene la ruta /swagger/index.html
    });
}

app.UseAuthentication();
app.UseAuthorization();
app.UseHttpsRedirection();
app.UseCors("frontend");
app.MapControllers();
app.Run();
