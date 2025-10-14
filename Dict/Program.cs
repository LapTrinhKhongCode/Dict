//var builder = WebApplication.CreateBuilder(args);

//// Add services to the container.

//builder.Services.AddControllers();
//// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
//builder.Services.AddEndpointsApiExplorer();
//builder.Services.AddSwaggerGen();

//var app = builder.Build();

//// Configure the HTTP request pipeline.
//if (app.Environment.IsDevelopment())
//{
//    app.UseSwagger();
//    app.UseSwaggerUI();
//}

//app.UseHttpsRedirection();

//app.UseAuthorization();

//app.MapControllers();

//app.Run();


using Dict.Data;
using Dict.Service;
using Dict.Service.IService;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;
using System.Text.RegularExpressions;

var builder = WebApplication.CreateBuilder(args);

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
              .AllowAnyHeader();  // Cho phép tất cả các headers
    });
});


builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHttpClient();
builder.Services.AddHttpClient<KanjiImportService>();
builder.Services.AddScoped<KanjiImportService>();
builder.Services.AddScoped<JsonService>();
builder.Services.AddScoped<WordImportService>();
builder.Services.AddScoped<IDeckService, DeckService>();
builder.Services.AddScoped<IReviewService, ReviewService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IJwtService, JwtService>();
builder.Services.AddScoped<IOcrJobService, OcrJobService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddSingleton<IBlobService, BlobService>();
builder.Services.AddScoped<IOcrProcessingService, OcrProcessingService>();

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
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
builder.Services.AddScoped<IJsonBuilderService, JsonBuilder>();
builder.Services.AddScoped<ISearchService, SearchService>();
builder.Services.AddScoped<IKanjiService, KanjiService>();
builder.Services.AddScoped<IWordService, WordService>();
builder.Services.AddScoped<ICommentService, CommentService>();
var app = builder.Build();

//using (var scope = app.Services.CreateScope())
//{
//    var json = scope.ServiceProvider.GetRequiredService<JsonService>();
//    await json.Serialize();
//}

// tạo scope để dùng DbContext và Service
//using (var scope = app.Services.CreateScope())
//{
//    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
//    var importer = scope.ServiceProvider.GetRequiredService<KanjiImportService>();

//    int total = await db.Entries.CountAsync();
//    int bigBatch = 500;

//    // Lấy toàn bộ Kanji
//    var allKanji = await db.Kanji
//        .OrderBy(x => x.Id) // quan trọng: đảm bảo offset đúng
//        .Select(x => x.Character)
//        .Distinct()
//        .ToListAsync();

//    for (int offset = 0; offset < total; offset += bigBatch)
//    {
//        var chunk = allKanji.Skip(offset).Take(bigBatch).ToArray();

//        try
//        {
//            await importer.RehydrateExamplesFromRawJsonAsync(
//                specificKanji: chunk,
//                batchLimit: chunk.Length
//            );
//            Console.WriteLine($"✅ Batch {offset} → {offset + bigBatch} done");
//        }
//        catch (Exception ex)
//        {
//            Console.WriteLine($"⚠️ Lỗi batch {offset} → {offset + bigBatch}, fallback từng entry...");

//            for (int i = 0; i < chunk.Length; i++)
//            {
//                try
//                {
//                    await importer.RehydrateExamplesFromRawJsonAsync(
//                        specificKanji: new[] { chunk[i] },
//                        batchLimit: 1
//                    );
//                    Console.WriteLine($"   ✅ Entry {offset + i} done");
//                }
//                catch (Exception innerEx)
//                {
//                    Console.WriteLine($"   ❌ Entry lỗi {offset + i}: {innerEx.Message}");
//                    // TODO: log ra file nếu cần
//                }
//            }
//        }
//    }
//}
//----------------------------------------------
//var tsvPath = args.Length > 0 ? args[0] : @"G:\N2\CODE\out.tsv";

//using (var scope = app.Services.CreateScope())
//{
//    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
//    var importer = scope.ServiceProvider.GetRequiredService<WordImportService>();

//    logger.LogInformation("Starting word import from TSV: {tsv}", tsvPath);

//    try
//    {
//        // Pass the application stopping token so import cancels when host is stopping
//        var token = app.Lifetime.ApplicationStopping;

//        // IMPORTANT: await the async call so the scope/DbContext stays alive for the duration
//        await importer.ImportWordsFromTsvAsync(tsvPath, token);

//        logger.LogInformation("Import finished successfully.");
//    }
//    catch (OperationCanceledException)
//    {
//        logger.LogWarning("Import cancelled by cancellation token.");
//    }
//    catch (Exception ex)
//    {
//        logger.LogError(ex, "Unhandled exception during import.");
//    }
//}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors("AllowAnyOrigin");

// SỬA LẠI THỨ TỰ Ở ĐÂY
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.Run();