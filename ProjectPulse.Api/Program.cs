using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Models;
using ProjectPulse.Api.DTOs;
using ProjectPulse.Api.Persistence;
using ProjectPulse.Api.Security;
using ProjectPulse.Api.Services;
using System.Text;
using System.Threading.RateLimiting;

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
builder.Services.AddOptions<JwtOptions>()
    .Bind(builder.Configuration.GetSection("Jwt"))
    .Validate(o => !string.IsNullOrWhiteSpace(o.Issuer), "Jwt:Issuer is required.")
    .Validate(o => !string.IsNullOrWhiteSpace(o.Audience), "Jwt:Audience is required.")
    .Validate(o => IsSafeJwtKey(o.Key),
        "Jwt:Key must be at least 32 UTF-8 bytes and must not be a placeholder or known demo key.")
    .ValidateOnStart();
builder.Services.AddScoped<IPasswordHasher, PasswordHasher>();
builder.Services.AddScoped<IJwtTokenService, JwtTokenService>();

// Rate limiting
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy(RateLimitPolicies.Auth, context =>
    {
        var client = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        var partitionKey = $"{client}:{context.Request.Path}";

        return RateLimitPartition.GetFixedWindowLimiter(
            partitionKey,
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 10,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0,
                AutoReplenishment = true
            });
    });
    options.OnRejected = async (context, cancellationToken) =>
    {
        await context.HttpContext.Response.WriteAsJsonAsync(new ProblemDetails
        {
            Status = StatusCodes.Status429TooManyRequests,
            Title = "Too many requests.",
            Detail = "Too many authentication attempts. Please try again later."
        }, cancellationToken);
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

// Authentication
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer();
builder.Services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
    .Configure<IOptions<JwtOptions>>((o, jwtOptions) =>
    {
        var jwt = jwtOptions.Value;
        o.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwt.Issuer,
            ValidAudience = jwt.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.Key))
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

if (string.IsNullOrWhiteSpace(connStr))
{
    if (!builder.Environment.IsDevelopment())
    {
        throw new InvalidOperationException(
            "ConnectionStrings:Default is required outside the Development environment.");
    }

    // Explicit development-only fallback for local SQLite use.
    connStr = "Data Source=projectpulse.db";
}

// Log seguro para ver qué se está usando en contenedor
builder.Logging.AddConsole();
builder.Logging.AddDebug();
builder.Services.AddSingleton(new { ConnectionStringInUse = connStr });

// Registra DbContext con la cadena final
builder.Services.AddDbContext<AppDbContext>(opt => opt.UseSqlite(connStr));

var app = builder.Build();

// Resolve validated options before startup tasks that can cause side effects.
_ = app.Services.GetRequiredService<IOptions<JwtOptions>>().Value;

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
app.UseRateLimiter();
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

static bool IsSafeJwtKey(string? key)
{
    if (string.IsNullOrWhiteSpace(key) || Encoding.UTF8.GetByteCount(key) < 32)
    {
        return false;
    }

    var value = key.Trim();
    if (value.StartsWith('<') && value.EndsWith('>'))
    {
        return false;
    }

    return !value.Equals("dev-super-secret-key-change-me-please-32chars-min", StringComparison.OrdinalIgnoreCase);
}

public partial class Program { }
