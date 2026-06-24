using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using ProjectPulse.Api.DTOs;
using ProjectPulse.Api.Persistence;
using ProjectPulse.Api.Services;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Controllers and validation
builder.Services.AddControllers();
builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddValidatorsFromAssemblyContaining<ProjectCreateDtoValidator>();
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

// Application services
builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection("Jwt"));
builder.Services.AddScoped<IPasswordHasher, PasswordHasher>();
builder.Services.AddScoped<IJwtTokenService, JwtTokenService>();

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

// Authentication
var jwt = builder.Configuration.GetSection("Jwt");
var key = Encoding.UTF8.GetBytes(jwt["Key"]!);

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(o =>
    {
        o.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwt["Issuer"],
            ValidAudience = jwt["Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(key)
        };
    });

// CORS
builder.Services.AddCors(o =>
{
    o.AddPolicy("frontend", p =>
        p.WithOrigins("http://localhost:5173", "http://localhost:3000")
         .AllowAnyHeader()
         .AllowAnyMethod());
});

// Persistence
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

// Database initialization
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
    var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("Seeder");
    await db.EnsureSeededAsync(hasher, logger);
}

// HTTP pipeline
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler("/error");
}

app.UseHttpsRedirection();
app.UseCors("frontend");
app.UseAuthentication();
app.UseAuthorization();

var enableSwagger = app.Environment.IsDevelopment()
                    || builder.Configuration.GetValue<bool>("EnableSwagger");

if (enableSwagger)
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "ProjectPulse API v1");
        c.RoutePrefix = "swagger";
    });
}

// Endpoints
app.Map("/error", (HttpContext _) =>
    Results.Problem(title: "Unexpected error", statusCode: StatusCodes.Status500InternalServerError));
app.MapControllers();

app.Run();
