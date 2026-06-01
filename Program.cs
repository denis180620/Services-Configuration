using System.Text;
using Configuration.Repository;
using Confuguration.Dbcontext;
using Confuguration.Services;
using Confuguration.ServicesSending;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCors(options =>
{
    options.AddPolicy("ReactApp", policy =>
    {
        policy.WithOrigins("http://localhost:3000")  
              .AllowAnyMethod()                      
              .AllowAnyHeader()                      
              .AllowCredentials();                   
    });
});

// Добавляем DbContext для PostgreSQL
builder.Services.AddDbContext<UserDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// Настраиваем ASP.NET Core Identity
builder.Services.AddIdentity<User, Role>(options =>
{
    // Настройки пароля
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireUppercase = true;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequiredLength = 6;

    // Настройки пользователя
    options.User.RequireUniqueEmail = true;
})
.AddEntityFrameworkStores<UserDbContext>()
.AddDefaultTokenProviders();

// Настраиваем JWT аутентификацию
var jwtSettings = builder.Configuration.GetSection("JwtSettings");
var secretKey = Encoding.UTF8.GetBytes(jwtSettings["Secret"] ?? throw new InvalidOperationException("JWT Secret not configured"));

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtSettings["Issuer"],
        ValidAudience = jwtSettings["Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(secretKey),
        ClockSkew = TimeSpan.Zero
    };

    // Настройки для работы с WebSocket и SignalR (если используется)
    options.Events = new JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            var accessToken = context.Request.Query["access_token"];
            var path = context.HttpContext.Request.Path;

            if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hubs"))
            {
                context.Token = accessToken;
            }

            return Task.CompletedTask;
        }
    };
});

builder.Services.AddAuthorization();

// Регистрируем Repository
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<ISessionUser, SessionUser>();
builder.Services.AddScoped<IContact, Contacts>();
builder.Services.AddScoped<IUserTemplateRepository, UserTemplateRepository>();
builder.Services.AddScoped<IUserHistoryRepository, RepositoryHistoryUser>();

// Регистрируем Services
builder.Services.AddScoped<IServiceAuthorization, ServiceAuthorization>();
builder.Services.AddScoped<ServicesContact, ServicesContact>();
builder.Services.AddScoped<ServicesTemplateUser, ServicesTemplateUser>();
builder.Services.AddScoped<ServicesHistory, ServicesHistory>();

// Регистрируем Messaging Senders
builder.Services.AddScoped<EmailSender, EmailSender>();
builder.Services.AddScoped<SmsSender, SmsSender>();
builder.Services.AddScoped<TelegramSender, TelegramSender>();
builder.Services.AddScoped<VkSender, VkSender>();
builder.Services.AddScoped<MessageSenderFactory, MessageSenderFactory>();

// Добавляем контроллеры и OpenAPI
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApi();

var app = builder.Build();

// ============ ПРИМЕНЕНИЕ CORS ============
// Включаем CORS перед другими middleware
app.UseCors("ReactApp");  // Используем политику "ReactApp"

// Или если нужно использовать несколько политик:
// app.UseCors("MultipleOrigins");

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

// Включаем аутентификацию и авторизацию
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
