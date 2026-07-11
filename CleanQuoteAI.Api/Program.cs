using Anthropic;
using CleanQuoteAI.Api.Data;
using CleanQuoteAI.Api.Endpoints;
using CleanQuoteAI.Api.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.AI;
using QuestPDF.Infrastructure;

QuestPDF.Settings.License = LicenseType.Community;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddProblemDetails();

builder.Services.AddCors(options => options.AddPolicy("frontend", policy => policy
    .WithOrigins("http://localhost:5173")
    .AllowAnyHeader()
    .AllowAnyMethod()));

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? "Host=localhost;Port=5432;Database=cleanquoteai;Username=admin;Password=admin123";
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(connectionString, npgsql => npgsql.UseVector()));

builder.Services.AddSingleton(_ =>
{
    // user-secrets d'abord, sinon le SDK résout ANTHROPIC_API_KEY / profil `ant auth login`
    var apiKey = builder.Configuration["Anthropic:ApiKey"];
    return apiKey is null ? new AnthropicClient() : new AnthropicClient { ApiKey = apiKey };
});

builder.Services.AddHttpClient<VoyageEmbeddingGenerator>(client =>
{
    client.BaseAddress = new Uri("https://api.voyageai.com/");
    client.Timeout = TimeSpan.FromSeconds(120);
});
builder.Services.AddScoped<IEmbeddingGenerator<string, Embedding<float>>>(
    sp => sp.GetRequiredService<VoyageEmbeddingGenerator>());

builder.Services.AddScoped<ConversationService>();
builder.Services.AddScoped<DevisService>();
builder.Services.AddScoped<RagService>();
builder.Services.AddSingleton<PdfService>();

var app = builder.Build();

app.UseExceptionHandler();
app.UseStatusCodePages();
app.UseCors("frontend");

// Migration EF Core + indexation RAG au démarrage
using (var scope = app.Services.CreateScope())
{
    var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("Startup");
    try
    {
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.MigrateAsync();
        logger.LogInformation("Base de données migrée");

        if (string.IsNullOrEmpty(app.Configuration["Voyage:ApiKey"]))
        {
            logger.LogWarning("Voyage:ApiKey manquante — indexation RAG ignorée. " +
                "Configurez-la avec : dotnet user-secrets set \"Voyage:ApiKey\" \"...\"");
        }
        else
        {
            var rag = scope.ServiceProvider.GetRequiredService<RagService>();
            await rag.EnsureIndexedAsync();
        }
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Initialisation base/RAG impossible — PostgreSQL est-il démarré ? (docker compose up -d)");
    }
}

app.MapGet("/", () => Results.Ok(new { app = "CleanQuote.AI", statut = "ok" }));
app.MapChatEndpoints();
app.MapDevisEndpoints();

app.Run();
