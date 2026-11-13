using ResumeOptimizer.Api.Configuration;
using ResumeOptimizer.Api.Infrastructure.Gemini;
using ResumeOptimizer.Api.Infrastructure.MongoDB;
using ResumeOptimizer.Api.Services;
using ResumeOptimizer.Api.Services.Interfaces;

namespace ResumeOptimizer.Api.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddResumeOptimizerServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Options
        services.Configure<MongoDbOptions>(configuration.GetSection(MongoDbOptions.Section));
        services.Configure<GeminiOptions>(configuration.GetSection(GeminiOptions.Section));
        services.Configure<AppOptions>(configuration.GetSection(AppOptions.Section));

        // Infrastructure
        services.AddSingleton<MongoDbContext>();
        services.AddHttpClient<GeminiClient>();

        // Repositories
        services.AddScoped<IAnalysisRepository, AnalysisRepository>();

        // Services
        services.AddScoped<IPdfParserService, PdfParserService>();
        services.AddScoped<IResumeStructuringService, ResumeStructuringService>();
        services.AddScoped<IChunkingService, ChunkingService>();
        services.AddScoped<IEmbeddingService, EmbeddingService>();
        services.AddScoped<IVectorSearchService, VectorSearchService>();
        services.AddScoped<IGenerationService, GenerationService>();
        services.AddScoped<AnalysisOrchestrator>();

        return services;
    }
}
