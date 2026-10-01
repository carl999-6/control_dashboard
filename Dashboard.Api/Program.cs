using System.Security.Claims;
using System.Text.RegularExpressions;
using System.Threading.RateLimiting;
using Dashboard.Api.Contracts;
using Dashboard.Api.Data;
using Dashboard.Api.Domain;
using Dashboard.Api.Endpoints;
using Dashboard.Api.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);
var dataProtectionDirectory = Path.Combine(builder.Environment.ContentRootPath, "Data", "keys");
Directory.CreateDirectory(dataProtectionDirectory);

builder.Logging.ClearProviders();
builder.Logging.AddSimpleConsole(options =>
{
    options.SingleLine = true;
    options.TimestampFormat = "HH:mm:ss ";
});

builder.Services.AddDbContext<DashboardDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("Dashboard")));
builder.Services.AddCors(options =>
{
    options.AddPolicy("LocalDashboard", policy =>
        policy.WithOrigins("http://localhost:5173", "http://127.0.0.1:5173")
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials());
});
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.Cookie.Name = "dashboard.local.session";
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Strict;
        options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.SlidingExpiration = true;
        options.Events.OnRedirectToLogin = context =>
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return Task.CompletedTask;
        };
        options.Events.OnRedirectToAccessDenied = context =>
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            return Task.CompletedTask;
        };
    });
builder.Services.AddAuthorization();
builder.Services.AddDataProtection()
    .SetApplicationName("Dashboard.Local")
    .PersistKeysToFileSystem(new DirectoryInfo(dataProtectionDirectory));
builder.Services.AddRateLimiter(options =>
{
    options.AddPolicy("login", context => RateLimitPartition.GetFixedWindowLimiter(
        context.Connection.RemoteIpAddress?.ToString() ?? "local",
        _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 5,
            Window = TimeSpan.FromMinutes(1),
            QueueLimit = 0,
        }));
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
});

var configuredPassword = builder.Configuration["DASHBOARD_ADMIN_PASSWORD"];
if (string.IsNullOrWhiteSpace(configuredPassword) && !builder.Environment.IsDevelopment())
{
    throw new InvalidOperationException("DASHBOARD_ADMIN_PASSWORD es obligatoria fuera del entorno de desarrollo.");
}

builder.Services.AddSingleton(new LocalAdminPasswordVerifier(configuredPassword ?? "control-local-2026"));
builder.Services.AddSingleton<INotificationChannel, SimulatedTelegramChannel>();

var app = builder.Build();

if (string.IsNullOrWhiteSpace(configuredPassword))
{
    app.Logger.LogWarning("Se usa la contraseña local de demostración. Configura DASHBOARD_ADMIN_PASSWORD antes de compartir el entorno.");
}

await using (var scope = app.Services.CreateAsyncScope())
{
    var db = scope.ServiceProvider.GetRequiredService<DashboardDbContext>();
    await db.Database.MigrateAsync();
    await SeedData.InitializeAsync(db);
}

app.UseCors("LocalDashboard");
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/api/health", () => Results.Ok(new
{
    status = "ok",
    service = "dashboard-api",
    environment = app.Environment.EnvironmentName,
    timestamp = DateTimeOffset.UtcNow,
}));

var auth = app.MapGroup("/api/auth");
auth.MapGet("/session", (ClaimsPrincipal user) => Results.Ok(new
{
    authenticated = user.Identity?.IsAuthenticated == true,
    displayName = user.Identity?.IsAuthenticated == true ? user.Identity.Name : null,
}));
auth.MapPost("/login", async (LoginRequest request, LocalAdminPasswordVerifier verifier, HttpContext context) =>
{
    if (!verifier.Verify(request.Password))
    {
        return Results.ValidationProblem(new Dictionary<string, string[]>
        {
            ["password"] = ["La contraseña no es válida."],
        });
    }

    var identity = new ClaimsIdentity(
        [new Claim(ClaimTypes.Name, "Administrador local"), new Claim(ClaimTypes.Role, "Administrator")],
        CookieAuthenticationDefaults.AuthenticationScheme);
    await context.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identity));
    return Results.Ok(new { authenticated = true, displayName = "Administrador local" });
}).RequireRateLimiting("login");
auth.MapPost("/logout", async (HttpContext context) =>
{
    await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
    return Results.NoContent();
}).RequireAuthorization();

var projects = app.MapGroup("/api/projects").RequireAuthorization();
projects.MapGet("/", async (DashboardDbContext db, CancellationToken cancellationToken) =>
    Results.Ok((await db.Projects.AsNoTracking().Include(project => project.Goals)
        .OrderBy(project => project.Name).ToListAsync(cancellationToken))
        .Select(ProjectResponse.FromEntity)));
projects.MapGet("/{id:guid}", async (Guid id, DashboardDbContext db, CancellationToken cancellationToken) =>
{
    var project = await db.Projects.AsNoTracking().Include(item => item.Goals)
        .SingleOrDefaultAsync(item => item.Id == id, cancellationToken);
    return project is null ? Results.NotFound() : Results.Ok(ProjectResponse.FromEntity(project));
});
projects.MapPost("/", async (ProjectRequest request, DashboardDbContext db, CancellationToken cancellationToken) =>
{
    var errors = ValidateProject(request);
    if (errors.Count > 0) return Results.ValidationProblem(errors);

    var now = DateTimeOffset.UtcNow;
    var project = new Project
    {
        Id = Guid.NewGuid(),
        Name = request.Name.Trim(),
        Slug = await CreateUniqueSlugAsync(request.Name, db, null, cancellationToken),
        Domain = request.Domain.Trim(),
        Type = request.Type.Trim(),
        Status = request.Status.Trim().ToLowerInvariant(),
        TimeZone = request.TimeZone.Trim(),
        Environment = request.Environment.Trim().ToLowerInvariant(),
        Description = request.Description?.Trim() ?? string.Empty,
        IsDemoData = false,
        CreatedAt = now,
        UpdatedAt = now,
    };
    db.Projects.Add(project);
    await db.SaveChangesAsync(cancellationToken);
    return Results.Created($"/api/projects/{project.Id}", ProjectResponse.FromEntity(project));
});
projects.MapPut("/{id:guid}", async (Guid id, ProjectRequest request, DashboardDbContext db, CancellationToken cancellationToken) =>
{
    var errors = ValidateProject(request);
    if (errors.Count > 0) return Results.ValidationProblem(errors);
    var project = await db.Projects.Include(item => item.Goals).SingleOrDefaultAsync(item => item.Id == id, cancellationToken);
    if (project is null) return Results.NotFound();

    project.Name = request.Name.Trim();
    project.Slug = await CreateUniqueSlugAsync(request.Name, db, id, cancellationToken);
    project.Domain = request.Domain.Trim();
    project.Type = request.Type.Trim();
    project.Status = request.Status.Trim().ToLowerInvariant();
    project.TimeZone = request.TimeZone.Trim();
    project.Environment = request.Environment.Trim().ToLowerInvariant();
    project.Description = request.Description?.Trim() ?? string.Empty;
    project.UpdatedAt = DateTimeOffset.UtcNow;
    await db.SaveChangesAsync(cancellationToken);
    return Results.Ok(ProjectResponse.FromEntity(project));
});
projects.MapDelete("/{id:guid}", async (Guid id, DashboardDbContext db, CancellationToken cancellationToken) =>
{
    var project = await db.Projects.SingleOrDefaultAsync(item => item.Id == id, cancellationToken);
    if (project is null) return Results.NotFound();
    db.Projects.Remove(project);
    await db.SaveChangesAsync(cancellationToken);
    return Results.NoContent();
});

projects.MapPost("/{projectId:guid}/goals", async (Guid projectId, GoalRequest request, DashboardDbContext db, CancellationToken cancellationToken) =>
{
    var errors = ValidateGoal(request);
    if (errors.Count > 0) return Results.ValidationProblem(errors);
    if (!await db.Projects.AnyAsync(item => item.Id == projectId, cancellationToken)) return Results.NotFound();
    var now = DateTimeOffset.UtcNow;
    var goal = new Goal
    {
        Id = Guid.NewGuid(), ProjectId = projectId, Title = request.Title.Trim(),
        Description = request.Description?.Trim() ?? string.Empty, Metric = request.Metric?.Trim() ?? string.Empty,
        TargetValue = request.TargetValue, Status = request.Status.Trim().ToLowerInvariant(), DueDate = request.DueDate,
        CreatedAt = now, UpdatedAt = now,
    };
    db.Goals.Add(goal);
    await db.SaveChangesAsync(cancellationToken);
    return Results.Created($"/api/projects/{projectId}/goals/{goal.Id}", GoalResponse.FromEntity(goal));
});
projects.MapPut("/{projectId:guid}/goals/{goalId:guid}", async (Guid projectId, Guid goalId, GoalRequest request, DashboardDbContext db, CancellationToken cancellationToken) =>
{
    var errors = ValidateGoal(request);
    if (errors.Count > 0) return Results.ValidationProblem(errors);
    var goal = await db.Goals.SingleOrDefaultAsync(item => item.ProjectId == projectId && item.Id == goalId, cancellationToken);
    if (goal is null) return Results.NotFound();
    goal.Title = request.Title.Trim(); goal.Description = request.Description?.Trim() ?? string.Empty;
    goal.Metric = request.Metric?.Trim() ?? string.Empty; goal.TargetValue = request.TargetValue;
    goal.Status = request.Status.Trim().ToLowerInvariant(); goal.DueDate = request.DueDate; goal.UpdatedAt = DateTimeOffset.UtcNow;
    await db.SaveChangesAsync(cancellationToken);
    return Results.Ok(GoalResponse.FromEntity(goal));
});
projects.MapDelete("/{projectId:guid}/goals/{goalId:guid}", async (Guid projectId, Guid goalId, DashboardDbContext db, CancellationToken cancellationToken) =>
{
    var goal = await db.Goals.SingleOrDefaultAsync(item => item.ProjectId == projectId && item.Id == goalId, cancellationToken);
    if (goal is null) return Results.NotFound();
    db.Goals.Remove(goal);
    await db.SaveChangesAsync(cancellationToken);
    return Results.NoContent();
});

var settings = app.MapGroup("/api/settings").RequireAuthorization();
settings.MapGet("/", async (DashboardDbContext db, CancellationToken cancellationToken) =>
    Results.Ok(await db.Preferences.AsNoTracking().SingleAsync(item => item.Id == 1, cancellationToken)));
settings.MapPut("/", async (PreferenceRequest request, DashboardDbContext db, CancellationToken cancellationToken) =>
{
    var errors = ValidatePreferences(request);
    if (errors.Count > 0) return Results.ValidationProblem(errors);
    var preference = await db.Preferences.SingleAsync(item => item.Id == 1, cancellationToken);
    preference.TimeZone = request.TimeZone.Trim(); preference.Currency = request.Currency.Trim().ToUpperInvariant();
    preference.DefaultDateRangeDays = request.DefaultDateRangeDays; preference.CompactNotifications = request.CompactNotifications;
    preference.UpdatedAt = DateTimeOffset.UtcNow;
    await db.SaveChangesAsync(cancellationToken);
    return Results.Ok(preference);
});

app.MapMarketingEndpoints();
app.MapSeoEndpoints();
app.MapExecutionEndpoints();
app.MapNotificationEndpoints();

app.Run();

static Dictionary<string, string[]> ValidateProject(ProjectRequest request)
{
    var errors = new Dictionary<string, string[]>();
    if (string.IsNullOrWhiteSpace(request.Name) || request.Name.Trim().Length > 120) errors["name"] = ["El nombre es obligatorio y admite hasta 120 caracteres."];
    if (string.IsNullOrWhiteSpace(request.Domain) || request.Domain.Trim().Length > 200) errors["domain"] = ["El dominio o identificador es obligatorio y admite hasta 200 caracteres."];
    if (string.IsNullOrWhiteSpace(request.Type)) errors["type"] = ["El tipo es obligatorio."];
    if (string.IsNullOrWhiteSpace(request.Status)) errors["status"] = ["El estado es obligatorio."];
    if (string.IsNullOrWhiteSpace(request.TimeZone)) errors["timeZone"] = ["La zona horaria es obligatoria."];
    if (string.IsNullOrWhiteSpace(request.Environment)) errors["environment"] = ["El entorno es obligatorio."];
    if ((request.Description?.Length ?? 0) > 600) errors["description"] = ["La descripción admite hasta 600 caracteres."];
    return errors;
}

static Dictionary<string, string[]> ValidateGoal(GoalRequest request)
{
    var errors = new Dictionary<string, string[]>();
    if (string.IsNullOrWhiteSpace(request.Title) || request.Title.Trim().Length > 160) errors["title"] = ["El título es obligatorio y admite hasta 160 caracteres."];
    if (string.IsNullOrWhiteSpace(request.Status)) errors["status"] = ["El estado es obligatorio."];
    if (request.TargetValue < 0) errors["targetValue"] = ["La meta no puede ser negativa."];
    return errors;
}

static Dictionary<string, string[]> ValidatePreferences(PreferenceRequest request)
{
    var errors = new Dictionary<string, string[]>();
    if (string.IsNullOrWhiteSpace(request.TimeZone)) errors["timeZone"] = ["La zona horaria es obligatoria."];
    if (!Regex.IsMatch(request.Currency?.Trim() ?? string.Empty, "^[A-Za-z]{3}$")) errors["currency"] = ["La moneda debe usar un código ISO de tres letras."];
    if (request.DefaultDateRangeDays is < 7 or > 365) errors["defaultDateRangeDays"] = ["El rango predeterminado debe estar entre 7 y 365 días."];
    return errors;
}

static async Task<string> CreateUniqueSlugAsync(string name, DashboardDbContext db, Guid? currentId, CancellationToken cancellationToken)
{
    var baseSlug = Regex.Replace(name.Trim().ToLowerInvariant().Normalize(System.Text.NormalizationForm.FormD), "[^a-z0-9]+", "-").Trim('-');
    if (string.IsNullOrWhiteSpace(baseSlug)) baseSlug = "proyecto";
    var slug = baseSlug;
    var suffix = 2;
    while (await db.Projects.AnyAsync(item => item.Slug == slug && item.Id != currentId, cancellationToken)) slug = $"{baseSlug}-{suffix++}";
    return slug;
}

public partial class Program;

