using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PathForge.Application.Problems;
using PathForge.Application.Evidence;
using PathForge.Application.History;
using PathForge.Application.ScopeItems;
using PathForge.Application.WorkLogs;
using PathForge.Application.KnowledgeDocuments;
using PathForge.Infrastructure.Persistence;
using PathForge.Infrastructure.Repositories;
using PathForge.Infrastructure.FileStorage;

namespace PathForge.Infrastructure.DependencyInjection;

public static class InfrastructureServiceCollectionExtensions
{
    public static IServiceCollection AddPathForgeInfrastructure(
        this IServiceCollection services,
        string connectionString,
        string evidenceDirectory,
        string knowledgeImagesDirectory)
    {
        services.AddDbContextFactory<PathForgeDbContext>(options =>
            options.UseSqlite(connectionString, sqlite =>
                sqlite.MigrationsAssembly(typeof(PathForgeDbContext).Assembly.FullName)));
        services.AddTransient<IProblemRepository, EfProblemRepository>();
        services.AddTransient<IProblemService, ProblemService>();
        services.AddTransient<IScopeItemRepository, EfScopeItemRepository>();
        services.AddTransient<IScopeItemService, ScopeItemService>();
        services.AddTransient<IWorkLogRepository, EfWorkLogRepository>();
        services.AddTransient<IWorkLogService, WorkLogService>();
        services.AddSingleton<IFileStorage>(_ => new LocalEvidenceStorage(evidenceDirectory));
        services.AddTransient<IEvidenceRepository, EfEvidenceRepository>();
        services.AddTransient<IEvidenceService, EvidenceService>();
        services.AddTransient<IHistoryRepository, EfHistoryRepository>();
        services.AddTransient<IHistoryService, HistoryService>();
        services.AddTransient<IKnowledgeDocumentRepository, EfKnowledgeDocumentRepository>();
        services.AddTransient<IKnowledgeDocumentService, KnowledgeDocumentService>();
        services.AddSingleton<IKnowledgeImageStorage>(_ => new LocalKnowledgeImageStorage(knowledgeImagesDirectory));
        return services;
    }
}
