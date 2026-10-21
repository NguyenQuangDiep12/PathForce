using System.IO;
using PathForge.Application.Problems;
using PathForge.Application.WorkLogs;
using PathForge.Domain.Entities;
using PathForge.Domain.Enums;
using PathForge.Domain.Rules;
using PathForge.Infrastructure.Persistence;
using PathForge.Infrastructure.Repositories;
using PathForge.Infrastructure.FileStorage;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Xunit;

namespace PathForge.Tests;

public sealed class BusinessRuleTests
{
    [Theory]
    [InlineData(ProblemStatus.NotStarted, ProblemStatus.InProgress, true)]
    [InlineData(ProblemStatus.NotStarted, ProblemStatus.Blocked, true)]
    [InlineData(ProblemStatus.InProgress, ProblemStatus.Blocked, true)]
    [InlineData(ProblemStatus.Blocked, ProblemStatus.InProgress, true)]
    [InlineData(ProblemStatus.InProgress, ProblemStatus.Resolved, true)]
    [InlineData(ProblemStatus.Blocked, ProblemStatus.Resolved, true)]
    [InlineData(ProblemStatus.Resolved, ProblemStatus.InProgress, true)]
    [InlineData(ProblemStatus.NotStarted, ProblemStatus.Resolved, false)]
    [InlineData(ProblemStatus.Resolved, ProblemStatus.Blocked, false)]
    public void ProblemTransitions_MatchTheBusinessRules(ProblemStatus current, ProblemStatus next, bool expected)
    {
        Assert.Equal(expected, ProblemStatusRules.CanTransition(current, next));
    }

    [Fact]
    public async Task ProblemCannotBeResolvedWithoutExplicitConfirmation()
    {
        var repository = new FakeProblemRepository(new Problem { Id = 7, Status = ProblemStatus.InProgress });
        var service = new ProblemService(repository);

        var withoutConfirmation = await service.ChangeStatusAsync(7, ProblemStatus.Resolved);

        Assert.False(withoutConfirmation.Succeeded);
        Assert.Equal(0, repository.UpdateCount);

        var confirmed = await service.ChangeStatusAsync(7, ProblemStatus.Resolved, userConfirmedResolution: true);

        Assert.True(confirmed.Succeeded);
        Assert.Equal(1, repository.UpdateCount);
    }

    [Fact]
    public async Task EmptyReportDoesNotCompleteAnAwaitingSession()
    {
        var log = new WorkLog { Id = 3, ProblemId = 2, TaskTitle = "Kiểm tra giả thuyết", Status = WorkLogStatus.AwaitingReport };
        var repository = new FakeWorkLogRepository(log);
        var service = new WorkLogService(repository, new FakeProblemService());

        var result = await service.SaveReportAsync(log.Id, new WorkLogReportDraft("Đã thử", " ", "", null, null));

        Assert.False(result.Succeeded);
        Assert.Equal(WorkLogStatus.AwaitingReport, log.Status);
        Assert.Equal(0, repository.UpdateCount);
    }

    [Fact]
    public async Task StartingFromScopeItemStoresItsLinkOnTheSession()
    {
        var repository = new FakeWorkLogRepository { ValidScopeItemId = 11 };
        var service = new WorkLogService(repository, new FakeProblemService());

        var result = await service.StartAsync(2, "Xác minh kết quả", 25, 11);

        Assert.True(result.Succeeded);
        Assert.Equal(11, repository.Added?.ScopeItemId);
        Assert.Equal("Xác minh kết quả", repository.Added?.TaskTitle);
    }

    [Fact]
    public async Task StartingFromScopeItemOfAnotherProblemIsRejected()
    {
        var repository = new FakeWorkLogRepository { ValidScopeItemId = 12 };
        var service = new WorkLogService(repository, new FakeProblemService());

        var result = await service.StartAsync(2, "Xác minh kết quả", 25, 11);

        Assert.False(result.Succeeded);
        Assert.Null(repository.Added);
    }

    [Fact]
    public async Task MigrationAddsOptionalFieldsWithoutLosingExistingRows()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<PathForgeDbContext>()
            .UseSqlite(connection, sqlite => sqlite.MigrationsAssembly(typeof(PathForgeDbContext).Assembly.FullName))
            .Options;

        await using var context = new PathForgeDbContext(options);
        var migrator = context.GetService<IMigrator>();
        await migrator.MigrateAsync("20261021000000_InitialCreate");
        var createdAt = DateTime.UtcNow;
        await context.Database.ExecuteSqlInterpolatedAsync(
            $"INSERT INTO Problems (Title, Goal, Status, RequiresEvidence, CreatedAt, UpdatedAt) VALUES ({"Existing problem"}, {"Keep it"}, {"InProgress"}, {false}, {createdAt}, {createdAt})");

        await context.Database.MigrateAsync();
        context.ChangeTracker.Clear();
        var saved = await context.Problems.SingleAsync(problem => problem.Id == 1);

        Assert.Equal("Existing problem", saved.Title);
        Assert.Equal("Keep it", saved.Goal);
        Assert.Null(saved.AnalysisWhat);
    }

    [Fact]
    public async Task DeletingScopeItemClearsItsOptionalWorkLogLinkAndKeepsTheReport()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<PathForgeDbContext>().UseSqlite(connection).Options;
        await using (var seed = new PathForgeDbContext(options))
        {
            await seed.Database.MigrateAsync();
            var problem = new Problem { Title = "Problem", Goal = "Goal", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
            var scope = new ScopeItem { Title = "Scope", Problem = problem, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
            var log = new WorkLog
            {
                Problem = problem, ScopeItemId = 1, TaskTitle = "Task", Result = "Report remains",
                StartedAt = DateTime.UtcNow, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow,
                Status = WorkLogStatus.Completed
            };
            problem.ScopeItems.Add(scope);
            problem.WorkLogs.Add(log);
            await seed.Problems.AddAsync(problem);
            await seed.SaveChangesAsync();
            log.ScopeItemId = scope.Id;
            await seed.SaveChangesAsync();
        }

        var repository = new EfScopeItemRepository(new TestContextFactory(options));
        await repository.DeleteAsync(1);

        await using var verify = new PathForgeDbContext(options);
        var remaining = await verify.WorkLogs.SingleAsync();
        Assert.Null(remaining.ScopeItemId);
        Assert.Equal("Report remains", remaining.Result);
    }

    [Fact]
    public async Task StagedEvidenceDeletionCanBeRolledBackOrCommitted()
    {
        var folder = Path.Combine(Path.GetTempPath(), "PathForge.Tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder);
        try
        {
            var storage = new LocalEvidenceStorage(folder);
            var originalPath = Path.Combine(folder, "sample.png");
            await File.WriteAllBytesAsync(originalPath, new byte[] { 1, 2, 3 });

            var staged = await storage.StageDeleteAsync("sample.png");
            Assert.NotNull(staged);
            Assert.False(File.Exists(originalPath));
            await storage.RestoreStagedAsync(staged!, "sample.png");
            Assert.True(File.Exists(originalPath));

            staged = await storage.StageDeleteAsync("sample.png");
            await storage.DeleteAsync(staged!);
            Assert.False(File.Exists(originalPath));
            Assert.False(File.Exists(storage.GetAbsolutePath(staged!)));
        }
        finally
        {
            if (Directory.Exists(folder))
                Directory.Delete(folder, recursive: true);
        }
    }

    private sealed class TestContextFactory : IDbContextFactory<PathForgeDbContext>
    {
        private readonly DbContextOptions<PathForgeDbContext> _options;
        public TestContextFactory(DbContextOptions<PathForgeDbContext> options) => _options = options;
        public PathForgeDbContext CreateDbContext() => new(_options);
    }

    private sealed class FakeProblemRepository : IProblemRepository
    {
        private readonly Problem _problem;
        public int UpdateCount { get; private set; }

        public FakeProblemRepository(Problem problem) => _problem = problem;
        public Task<IReadOnlyList<Problem>> SearchAsync(string? keyword, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<Problem>>(new[] { _problem });
        public Task<Problem?> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
            Task.FromResult<Problem?>(id == _problem.Id ? _problem : null);
        public Task<int> CountEvidenceAsync(int problemId, CancellationToken cancellationToken = default) => Task.FromResult(0);
        public Task<int> AddAsync(Problem problem, CancellationToken cancellationToken = default) => Task.FromResult(1);
        public Task UpdateAsync(Problem problem, CancellationToken cancellationToken = default)
        {
            UpdateCount++;
            return Task.CompletedTask;
        }
        public Task<IReadOnlyList<string>> DeleteAsync(int id, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<string>>(Array.Empty<string>());
    }

    private sealed class FakeProblemService : IProblemService
    {
        private ProblemStatus _status = ProblemStatus.NotStarted;
        public Task<IReadOnlyList<ProblemListItem>> SearchAsync(string? keyword, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<ProblemListItem>>(Array.Empty<ProblemListItem>());
        public Task<ProblemDetails?> GetAsync(int id, CancellationToken cancellationToken = default) =>
            Task.FromResult<ProblemDetails?>(new ProblemDetails(id, "Bài toán", "Mục tiêu", null, null, null, null, null, null, null, null, null, _status, false, DateTime.UtcNow, DateTime.UtcNow));
        public Task<ProblemDetails?> GetCurrentAsync(CancellationToken cancellationToken = default) => GetAsync(2, cancellationToken);
        public Task<ProblemOperationResult> SaveAsync(int? id, ProblemDraft draft, CancellationToken cancellationToken = default) =>
            Task.FromResult(ProblemOperationResult.Success(id ?? 2));
        public Task<ProblemOperationResult> ChangeStatusAsync(int id, ProblemStatus status, bool userConfirmedResolution = false, CancellationToken cancellationToken = default)
        {
            _status = status;
            return Task.FromResult(ProblemOperationResult.Success(id));
        }
        public Task<ProblemOperationResult> DeleteAsync(int id, CancellationToken cancellationToken = default) =>
            Task.FromResult(ProblemOperationResult.Success(id));
    }

    private sealed class FakeWorkLogRepository : IWorkLogRepository
    {
        private readonly WorkLog? _existing;
        public int? ValidScopeItemId { get; set; }
        public int UpdateCount { get; private set; }
        public WorkLog? Added { get; private set; }

        public FakeWorkLogRepository(WorkLog? existing = null) => _existing = existing;
        public Task<IReadOnlyList<WorkLog>> GetHistoryAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<WorkLog>>(Array.Empty<WorkLog>());
        public Task<IReadOnlyList<WorkLog>> GetByProblemAsync(int problemId, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<WorkLog>>(Array.Empty<WorkLog>());
        public Task<WorkLog?> GetOpenAsync(CancellationToken cancellationToken = default) => Task.FromResult(_existing);
        public Task<WorkLog?> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
            Task.FromResult<WorkLog?>(_existing?.Id == id ? _existing : Added?.Id == id ? Added : null);
        public Task<bool> HasRunningAsync(CancellationToken cancellationToken = default) => Task.FromResult(false);
        public Task<bool> ScopeItemBelongsToProblemAsync(int scopeItemId, int problemId, CancellationToken cancellationToken = default) =>
            Task.FromResult(scopeItemId == ValidScopeItemId);
        public Task<int> AddAsync(WorkLog workLog, CancellationToken cancellationToken = default)
        {
            workLog.Id = 9;
            Added = workLog;
            return Task.FromResult(workLog.Id);
        }
        public Task UpdateAsync(WorkLog workLog, CancellationToken cancellationToken = default)
        {
            UpdateCount++;
            return Task.CompletedTask;
        }
    }
}
