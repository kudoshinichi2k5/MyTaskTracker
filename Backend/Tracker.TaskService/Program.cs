using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Cấu hình JWT Bearer Authentication
var jwtSettings = builder.Configuration.GetSection("JwtSettings");

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

var app = builder.Build();
app.UseCors("AllowFrontend");
app.UseAuthentication();
app.UseAuthorization();

// Mock Data In-Memory để test trước khi gắn SQLite
var tasks = new List<TaskItem>
{
    new TaskItem { Id = 1, Title = "Set up local IIS", IsCompleted = false },
    new TaskItem { Id = 2, Title = "Learn microservices", IsCompleted = true }
};

// Endpoint lấy task (BẮT BUỘC CÓ TOKEN)
app.MapGet("/api/v1/tasks", () =>
{
    return Results.Ok(tasks);
}).RequireAuthorization();

app.Run(); // Kestrel tự lấy URL từ appsettings ("Urls") hoặc biến môi trường ASPNETCORE_URLS

public class TaskItem
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public bool IsCompleted { get; set; }
}