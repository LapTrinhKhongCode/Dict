using Dict.Data;
using Dict.Middleware;
using Dict.Models;
using Dict.Service;
using Dict.Service.IService;
using Dict.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;
using System.Text.RegularExpressions;

var builder = WebApplication.CreateBuilder(args);
builder.WebHost.ConfigureKestrel(options =>
{
    options.Limits.MaxRequestBodySize = 500 * 1024 * 1024; // 500MB
});
builder.Services.Configure<Microsoft.AspNetCore.Http.Features.FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = 500 * 1024 * 1024; // 500MB
});

builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "My API", Version = "v1" });

    // ⚙️ Thêm định nghĩa Bearer token
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "Nhập token JWT theo dạng: Bearer {token}",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    // ⚙️ Yêu cầu Swagger thêm nút Authorize
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                },
                Scheme = "oauth2",
                Name = "Bearer",
                In = ParameterLocation.Header
            },
            new List<string>()
        }
    });
});

// Add services to the container.
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAnyOrigin", policy =>
    {
        policy.AllowAnyOrigin()  // Cho phép tất cả các domain
              .AllowAnyMethod()  // Cho phép tất cả các phương thức HTTP
              .AllowAnyHeader(); // Cho phép tất cả các headers
    });
});
builder.Services.AddHttpContextAccessor();

// --- Cấu hình Identity và Authentication (KHỐI CHUẨN) ---
builder.Services.AddIdentity<ApplicationUser, ApplicationRole>(options =>
{
    // Cấu hình mật khẩu (tùy chọn)
    options.Password.RequireDigit = false;
    options.Password.RequireLowercase = false;
    options.Password.RequireUppercase = false;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequiredLength = 6;

    // Cấu hình người dùng
    options.User.RequireUniqueEmail = true;

    // Cấu hình đăng nhập
    options.SignIn.RequireConfirmedAccount = false; // Tắt nếu bạn không dùng xác thực email
})
.AddEntityFrameworkStores<ApplicationDbContext>() // Chỉ định DbContext
.AddDefaultTokenProviders(); // Thêm dịch vụ tạo token (ví dụ: reset mật khẩu)

// 2. Cấu hình Xác thực (Authentication) để dùng JWT
// Chúng ta cần AddAuthentication SAU KHI AddIdentity
// và cấu hình nó để sử dụng JWT làm mặc định, thay vì Cookie
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options => // Giữ nguyên cấu hình AddJwtBearer cũ của bạn
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = builder.Configuration["Jwt:Issuer"],
        ValidAudience = builder.Configuration["Jwt:Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]))
    };
});
// --- Kết thúc khối Identity/Auth ---

builder.Services.AddControllers();
builder.Services.AddMemoryCache();
builder.Services.AddEndpointsApiExplorer();
// ĐÃ XÓA: builder.Services.AddSwaggerGen(); (bị lặp)
builder.Services.AddHttpClient();
builder.Services.AddHttpClient<KanjiImportService>();

// --- Đăng ký các dịch vụ (Services) của ứng dụng ---
builder.Services.AddScoped<KanjiImportService>();
builder.Services.AddScoped<JsonService>();
builder.Services.AddScoped<WordImportService>();
builder.Services.AddScoped<IDeckService, DeckService>();
builder.Services.AddScoped<IReviewService, ReviewService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IJwtService, JwtService>();
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();
builder.Services.AddScoped<IOcrJobService, OcrJobService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddSingleton<IBlobService, BlobService>();
builder.Services.AddScoped<IOcrProcessingService, OcrProcessingService>();
builder.Services.AddScoped<ISubscriptionService, SubscriptionService>();
builder.Services.AddScoped<IAdminService, AdminService>();

builder.Services.AddScoped<IProjectService, ProjectService>();
// Program.cs
builder.Services.AddScoped<IWorkspaceService, WorkspaceService>();

// << --- ĐÃ XÓA KHỐI AddAuthentication BỊ LẶP Ở ĐÂY --- >>

builder.Services.AddScoped<IJsonBuilderService, JsonBuilder>();
builder.Services.AddScoped<ISearchService, SearchService>();
builder.Services.AddScoped<IKanjiService, KanjiService>();
builder.Services.AddScoped<IWordService, WordService>();
builder.Services.AddScoped<ICommentService, CommentService>();
builder.Services.AddSingleton<AzureTokenProvider>();
var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseSwagger();
app.UseSwaggerUI();
if (app.Environment.IsDevelopment())
{

}
app.UseStaticFiles();
//app.UseHttpsRedirection();
app.UseCors("AllowAnyOrigin");
app.UseStaticFiles();
app.UseMiddleware<ApiLoggingMiddleware>();
// Thứ tự này là CHUẨN XÁC
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
//using (var scope = app.Services.CreateScope())
//{
//    var services = scope.ServiceProvider;
//    var logger = services.GetRequiredService<ILogger<Program>>();
//    try
//    {
//        // (Tùy chọn: Tự động chạy Migration nếu DB chưa được cập nhật)
//        // var dbContext = services.GetRequiredService<ApplicationDbContext>();
//        // await dbContext.Database.MigrateAsync();

//        // Gọi hàm Seed Data và chờ nó hoàn thành
//        // Chúng ta dùng GetAwaiter().GetResult() để chạy async trong ngữ cảnh sync
//        DbSeeder.SeedRolesAndAdminAsync(services).GetAwaiter().GetResult();

//        logger.LogInformation("Khởi tạo (Seed) Database thành công.");
//    }
//    catch (Exception ex)
//    {
//        // Log lỗi nghiêm trọng nếu Seed Data thất bại
//        logger.LogError(ex, "Đã xảy ra lỗi nghiêm trọng khi Khởi tạo (Seed) Database.");
//    }
//}
app.Run();
public partial class Program { }