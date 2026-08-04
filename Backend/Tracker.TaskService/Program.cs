using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Cấu hình JWT Bearer Authentication
var jwtSettings = builder.Configuration.GetSection("JwtSettings");

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        // Trỏ URL về Identity Provider
        options.Authority = builder.Configuration["JwtSettings:Authority"];
        options.Audience = builder.Configuration["JwtSettings:Audience"];
        
        // Bắt buộc set false ở Phase 1/2 vì test Local không có HTTPS
        options.RequireHttpsMetadata = false; 
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
    new TaskItem { Id = 1, Title = "Setup IIS Local", IsCompleted = false },
    new TaskItem { Id = 2, Title = "Học Microservices", IsCompleted = true }
};

// Endpoint lấy task (BẮT BUỘC CÓ TOKEN)
app.MapGet("/api/v1/tasks", () =>
{
    return Results.Ok(tasks);
}).RequireAuthorization();

app.Run("http://localhost:5002"); // Ép chạy port 5002 cho Task Service

public class TaskItem
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public bool IsCompleted { get; set; }
}