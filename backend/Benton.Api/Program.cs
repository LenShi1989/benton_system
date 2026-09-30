using System.Text;
using System.Text.Json;
using Benton.Api.Data;
using Benton.Api.Models;
using Benton.Api.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// 1. Add DbContext
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// 2. Add Services
builder.Services.AddScoped<IJwtService, JwtService>();
builder.Services.AddScoped<IAuditLogService, AuditLogService>();

// 3. Configure JWT Authentication
var jwtSettings = builder.Configuration.GetSection("Jwt");
var secret = jwtSettings["Secret"] ?? "BentonSystemSuperSecretJwtKey2026!ForAuthenticationAndSecurity#99";
var key = Encoding.UTF8.GetBytes(secret);

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.RequireHttpsMetadata = false;
    options.SaveToken = true;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtSettings["Issuer"] ?? "BentonApi",
        ValidAudience = jwtSettings["Audience"] ?? "BentonWeb",
        IssuerSigningKey = new SymmetricSecurityKey(key)
    };
});

builder.Services.AddAuthorization();
builder.Services.AddControllers();

// 4. Configure CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

// 5. Configure Swagger / OpenAPI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "訂便當系統 API (Benton.Api)", Version = "v1" });
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "請輸入 JWT Token (格式: Bearer {your token})",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

var app = builder.Build();

// 6. Ensure Database Created & Seeded
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    try
    {
        db.Database.EnsureCreated();

        // Ensure newly added tables and columns exist even if database already existed
        db.Database.ExecuteSqlRaw(@"
            CREATE TABLE IF NOT EXISTS ""Roles"" (
                ""Id"" SERIAL PRIMARY KEY,
                ""Name"" VARCHAR(50) NOT NULL UNIQUE,
                ""Description"" VARCHAR(255),
                ""AllowedMenus"" TEXT NOT NULL,
                ""CreatedAt"" TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
            );
            ALTER TABLE ""Users"" ADD COLUMN IF NOT EXISTS ""RoleId"" INT REFERENCES ""Roles""(""Id"");
            CREATE TABLE IF NOT EXISTS ""AuditLogs"" (
                ""Id"" SERIAL PRIMARY KEY,
                ""UserId"" INT,
                ""UserName"" VARCHAR(100) NOT NULL,
                ""Action"" VARCHAR(100) NOT NULL,
                ""Details"" TEXT,
                ""IpAddress"" VARCHAR(50),
                ""CreatedAt"" TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
            );
        ");

        // Seed Default Roles if empty
        if (!db.Roles.Any())
        {
            var adminRole = new Role
            {
                Name = "Admin",
                Description = "系統最高管理員，擁有所有選單與操作權限",
                AllowedMenus = JsonSerializer.Serialize(new[] { "/", "/orders", "/stores", "/users", "/roles", "/logs" })
            };
            var userRole = new Role
            {
                Name = "User",
                Description = "一般員工，可參與便當團購點餐與檢視個人明細",
                AllowedMenus = JsonSerializer.Serialize(new[] { "/", "/orders" })
            };
            var managerRole = new Role
            {
                Name = "Manager",
                Description = "便當團購專員，可發起團購與管理便當店家",
                AllowedMenus = JsonSerializer.Serialize(new[] { "/", "/orders", "/stores" })
            };

            db.Roles.AddRange(adminRole, userRole, managerRole);
            db.SaveChanges();
        }

        // Seed Default Admin User if empty
        if (!db.Users.Any())
        {
            var adminRole = db.Roles.FirstOrDefault(r => r.Name == "Admin");
            var userRole = db.Roles.FirstOrDefault(r => r.Name == "User");

            db.Users.AddRange(
                new User
                {
                    Username = "admin",
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("admin123"),
                    FullName = "系統管理員",
                    Role = "Admin",
                    RoleId = adminRole?.Id
                },
                new User
                {
                    Username = "user1",
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("user123"),
                    FullName = "張小明",
                    Role = "User",
                    RoleId = userRole?.Id
                },
                new User
                {
                    Username = "user2",
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("user123"),
                    FullName = "陳大華",
                    Role = "User",
                    RoleId = userRole?.Id
                }
            );
            db.SaveChanges();
        }

        // Seed Sample Stores if empty
        if (!db.Stores.Any())
        {
            var store1 = new Store
            {
                Name = "池上排骨飯",
                Phone = "02-2345-6789",
                Address = "台北市中山區南京東路二段100號",
                IsActive = true
            };
            var store2 = new Store
            {
                Name = "正宗台南雞腿便當",
                Phone = "02-8765-4321",
                Address = "台北市大安區信義路四段50號",
                IsActive = true
            };
            db.Stores.AddRange(store1, store2);
            db.SaveChanges();

            db.MenuItems.AddRange(
                new MenuItem { StoreId = store1.Id, Name = "招牌排骨飯", Description = "香酥排骨搭配特製配菜", Price = 110, IsActive = true },
                new MenuItem { StoreId = store1.Id, Name = "紅燒牛肉飯", Description = "軟嫩牛肉佐濃郁紅燒醬汁", Price = 130, IsActive = true },
                new MenuItem { StoreId = store1.Id, Name = "香草雞腿飯", Description = "酥脆炸雞腿便當", Price = 120, IsActive = true },
                new MenuItem { StoreId = store1.Id, Name = "素菜便當", Description = "多款時令蔬菜與豆干", Price = 90, IsActive = true },
                new MenuItem { StoreId = store2.Id, Name = "特級大雞腿飯", Description = "超大外酥內嫩雞腿", Price = 125, IsActive = true },
                new MenuItem { StoreId = store2.Id, Name = "滷排骨飯", Description = "傳統古早味古法滷排骨", Price = 105, IsActive = true },
                new MenuItem { StoreId = store2.Id, Name = "蒲燒鰻魚飯", Description = "特調蒲燒醬香烤鰻魚", Price = 160, IsActive = true }
            );
            db.SaveChanges();
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Database initialization error: {ex.Message}");
    }
}

// 7. Middleware pipeline
if (app.Environment.IsDevelopment() || true)
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Benton API v1");
    });
}

app.UseCors("AllowAll");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();
