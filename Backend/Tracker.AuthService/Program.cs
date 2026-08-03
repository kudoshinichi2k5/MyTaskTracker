using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Cấu hình CORS cho Angular gọi vào
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

// Mock Data cho User (Sau này bạn có thể thay bằng SQLite/SQL Server)
app.MapPost("/api/v1/auth/login", (LoginRequest request, IConfiguration config) =>
{
    // Validate tài khoản cứng (để test Phase 1)
    if (request.Username == "admin" && request.Password == "123456")
    {
        var token = GenerateJwtToken(request.Username, config);
        return Results.Ok(new { status = "success", token = token });
    }
    return Results.Unauthorized();
});

app.Run("http://localhost:5001"); // Ép chạy port 5001 cho Auth

// Hàm tạo JWT Token
string GenerateJwtToken(string username, IConfiguration config)
{
    var jwtSettings = config.GetSection("JwtSettings");
    var key = Encoding.UTF8.GetBytes(jwtSettings["SecretKey"]!);
    
    var claims = new[] { new Claim(ClaimTypes.Name, username) };
    
    var tokenDescriptor = new SecurityTokenDescriptor
    {
        Subject = new ClaimsIdentity(claims),
        Expires = DateTime.UtcNow.AddHours(2), // Token sống 2 tiếng
        Issuer = jwtSettings["Issuer"],
        Audience = jwtSettings["Audience"],
        SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
    };
    
    var tokenHandler = new JwtSecurityTokenHandler();
    var token = tokenHandler.CreateToken(tokenDescriptor);
    return tokenHandler.WriteToken(token);
}

public class LoginRequest
{
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}