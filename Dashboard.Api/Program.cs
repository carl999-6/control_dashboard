var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCors(options =>
{
    options.AddPolicy("LocalDashboard", policy =>
        policy.WithOrigins("http://localhost:5173")
            .AllowAnyHeader()
            .AllowAnyMethod());
});

var app = builder.Build();

app.UseCors("LocalDashboard");

app.MapGet("/api/health", () => Results.Ok(new
{
    status = "ok",
    service = "dashboard-api",
    environment = app.Environment.EnvironmentName,
    timestamp = DateTimeOffset.UtcNow,
}));

app.Run();

