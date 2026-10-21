using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace PathForge.Infrastructure.Persistence.Migrations;

[DbContext(typeof(PathForgeDbContext))]
[Migration("20261021000000_InitialCreate")]
public sealed class InitialCreate : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "Problems",
            columns: table => new
            {
                Id = table.Column<int>(type: "INTEGER", nullable: false).Annotation("Sqlite:Autoincrement", true),
                Title = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                Goal = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: false),
                Description = table.Column<string>(type: "TEXT", maxLength: 4000, nullable: true),
                SuccessCriteria = table.Column<string>(type: "TEXT", maxLength: 4000, nullable: true),
                NextAction = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: true),
                Status = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                RequiresEvidence = table.Column<bool>(type: "INTEGER", nullable: false),
                CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
            },
            constraints: table => table.PrimaryKey("PK_Problems", problem => problem.Id));

        migrationBuilder.CreateTable(
            name: "ScopeItems",
            columns: table => new
            {
                Id = table.Column<int>(type: "INTEGER", nullable: false).Annotation("Sqlite:Autoincrement", true),
                ProblemId = table.Column<int>(type: "INTEGER", nullable: false),
                Title = table.Column<string>(type: "TEXT", maxLength: 300, nullable: false),
                Reason = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: true),
                Status = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_ScopeItems", item => item.Id);
                table.ForeignKey("FK_ScopeItems_Problems_ProblemId", item => item.ProblemId,
                    "Problems", "Id", onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "WorkLogs",
            columns: table => new
            {
                Id = table.Column<int>(type: "INTEGER", nullable: false).Annotation("Sqlite:Autoincrement", true),
                ProblemId = table.Column<int>(type: "INTEGER", nullable: false),
                TaskTitle = table.Column<string>(type: "TEXT", maxLength: 300, nullable: false),
                PlannedDurationMinutes = table.Column<int>(type: "INTEGER", nullable: true),
                StartedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                EndedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                ActiveSeconds = table.Column<int>(type: "INTEGER", nullable: false),
                LastResumedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                Status = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                WhatWasDone = table.Column<string>(type: "TEXT", maxLength: 4000, nullable: true),
                Result = table.Column<string>(type: "TEXT", maxLength: 4000, nullable: true),
                Learned = table.Column<string>(type: "TEXT", maxLength: 4000, nullable: true),
                Blocker = table.Column<string>(type: "TEXT", maxLength: 4000, nullable: true),
                NextStep = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: true),
                CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_WorkLogs", log => log.Id);
                table.ForeignKey("FK_WorkLogs_Problems_ProblemId", log => log.ProblemId,
                    "Problems", "Id", onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "Evidence",
            columns: table => new
            {
                Id = table.Column<int>(type: "INTEGER", nullable: false).Annotation("Sqlite:Autoincrement", true),
                ProblemId = table.Column<int>(type: "INTEGER", nullable: false),
                WorkLogId = table.Column<int>(type: "INTEGER", nullable: true),
                DisplayName = table.Column<string>(type: "TEXT", maxLength: 260, nullable: false),
                RelativePath = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                Description = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: true),
                CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Evidence", evidence => evidence.Id);
                table.ForeignKey("FK_Evidence_Problems_ProblemId", evidence => evidence.ProblemId,
                    "Problems", "Id", onDelete: ReferentialAction.Cascade);
                table.ForeignKey("FK_Evidence_WorkLogs_WorkLogId", evidence => evidence.WorkLogId,
                    "WorkLogs", "Id", onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex("IX_ScopeItems_ProblemId", "ScopeItems", "ProblemId");
        migrationBuilder.CreateIndex("IX_WorkLogs_ProblemId", "WorkLogs", "ProblemId");
        migrationBuilder.CreateIndex("IX_WorkLogs_Status_Running", "WorkLogs", "Status", unique: true, filter: "\"Status\" = 'Running'");
        migrationBuilder.CreateIndex("IX_Evidence_ProblemId", "Evidence", "ProblemId");
        migrationBuilder.CreateIndex("IX_Evidence_WorkLogId", "Evidence", "WorkLogId");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "Evidence");
        migrationBuilder.DropTable(name: "ScopeItems");
        migrationBuilder.DropTable(name: "WorkLogs");
        migrationBuilder.DropTable(name: "Problems");
    }
}
