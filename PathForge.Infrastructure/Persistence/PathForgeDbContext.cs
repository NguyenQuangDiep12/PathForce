using Microsoft.EntityFrameworkCore;
using PathForge.Domain.Entities;

namespace PathForge.Infrastructure.Persistence;

public sealed class PathForgeDbContext : DbContext
{
    public PathForgeDbContext(DbContextOptions<PathForgeDbContext> options) : base(options) { }

    public DbSet<Problem> Problems => Set<Problem>();
    public DbSet<ScopeItem> ScopeItems => Set<ScopeItem>();
    public DbSet<WorkLog> WorkLogs => Set<WorkLog>();
    public DbSet<Evidence> Evidence => Set<Evidence>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Problem>(entity =>
        {
            entity.ToTable("Problems");
            entity.HasKey(problem => problem.Id);
            entity.Property(problem => problem.Title).IsRequired().HasMaxLength(200);
            entity.Property(problem => problem.Goal).IsRequired().HasMaxLength(2000);
            entity.Property(problem => problem.Description).HasMaxLength(4000);
            entity.Property(problem => problem.SuccessCriteria).HasMaxLength(4000);
            entity.Property(problem => problem.NextAction).HasMaxLength(2000);
            entity.Property(problem => problem.Status).HasConversion<string>().HasMaxLength(32);
            entity.HasMany(problem => problem.ScopeItems).WithOne(item => item.Problem)
                .HasForeignKey(item => item.ProblemId).OnDelete(DeleteBehavior.Cascade);
            entity.HasMany(problem => problem.WorkLogs).WithOne(log => log.Problem)
                .HasForeignKey(log => log.ProblemId).OnDelete(DeleteBehavior.Cascade);
            entity.HasMany(problem => problem.EvidenceItems).WithOne(evidence => evidence.Problem)
                .HasForeignKey(evidence => evidence.ProblemId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ScopeItem>(entity =>
        {
            entity.ToTable("ScopeItems");
            entity.HasKey(item => item.Id);
            entity.Property(item => item.Title).IsRequired().HasMaxLength(300);
            entity.Property(item => item.Reason).HasMaxLength(2000);
            entity.Property(item => item.Status).HasConversion<string>().HasMaxLength(32);
        });

        modelBuilder.Entity<WorkLog>(entity =>
        {
            entity.ToTable("WorkLogs");
            entity.HasKey(log => log.Id);
            entity.Property(log => log.TaskTitle).IsRequired().HasMaxLength(300);
            entity.Property(log => log.Status).HasConversion<string>().HasMaxLength(32);
            entity.HasIndex(log => log.Status).IsUnique().HasFilter("\"Status\" = 'Running'");
            entity.Property(log => log.WhatWasDone).HasMaxLength(4000);
            entity.Property(log => log.Result).HasMaxLength(4000);
            entity.Property(log => log.Learned).HasMaxLength(4000);
            entity.Property(log => log.Blocker).HasMaxLength(4000);
            entity.Property(log => log.NextStep).HasMaxLength(2000);
        });

        modelBuilder.Entity<Evidence>(entity =>
        {
            entity.ToTable("Evidence");
            entity.HasKey(item => item.Id);
            entity.Property(item => item.DisplayName).IsRequired().HasMaxLength(260);
            entity.Property(item => item.RelativePath).IsRequired().HasMaxLength(500);
            entity.Property(item => item.Description).HasMaxLength(2000);
            entity.HasOne(item => item.WorkLog).WithMany(log => log.EvidenceItems)
                .HasForeignKey(item => item.WorkLogId).OnDelete(DeleteBehavior.Cascade);
        });
    }
}
