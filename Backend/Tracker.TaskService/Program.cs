using Microsoft.AspNetCore.Authentication.JwtBearer;
using Tracker.TaskService.Endpoints;
using Tracker.TaskService.Services;

var builder = WebApplication.CreateBuilder(args);

// Cấu hình JWT Bearer Authentication
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.Authority = builder.Configuration["JwtSettings:Authority"];
        options.Audience = builder.Configuration["JwtSettings:Audience"];

        // Đọc trực tiếp từ config thay vì suy đoán theo tên môi trường,
        // vì Testing/Staging đôi khi vẫn cần trỏ về Keycloak local (HTTP).
        options.RequireHttpsMetadata = builder.Configuration.GetValue<bool>("JwtSettings:RequireHttpsMetadata");
    });
builder.Services.AddAuthorization();

// Cấu hình CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        // Lấy URL từ file appsettings tương ứng với môi trường
        var frontendUrl = builder.Configuration["FrontendUrl"] ?? "http://localhost:4200";

        policy.WithOrigins(frontendUrl)
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

// In-memory task store, scoped per user. Registered as a singleton so all
// requests share the same in-process dictionary (see InMemoryTaskStore for
// the persistence caveat).
builder.Services.AddSingleton<ITaskStore, InMemoryTaskStore>();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (!app.Environment.IsProduction())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("AllowFrontend");
app.UseAuthentication();
app.UseAuthorization();

// Lightweight liveness endpoint - useful for IIS/App Init monitoring and
// for smoke-testing each of the three environments (Testing/Staging/
// Production) after deployment without needing a valid JWT.
app.MapGet("/health", () => Results.Ok(new { status = "healthy", environment = app.Environment.EnvironmentName }));

app.MapTaskEndpoints();

app.Run(); // Kestrel tự lấy URL từ appsettings ("Urls") hoặc biến môi trường ASPNETCORE_URLS