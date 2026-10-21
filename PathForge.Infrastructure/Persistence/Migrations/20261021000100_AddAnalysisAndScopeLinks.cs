using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace PathForge.Infrastructure.Persistence.Migrations;

[DbContext(typeof(PathForgeDbContext))]
[Migration("20261021000100_AddAnalysisAndScopeLinks")]
public sealed class AddAnalysisAndScopeLinks : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(name: "AnalysisWhat", table: "Problems", type: "TEXT", maxLength: 2000, nullable: true);
        migrationBuilder.AddColumn<string>(name: "AnalysisWhy", table: "Problems", type: "TEXT", maxLength: 2000, nullable: true);
        migrationBuilder.AddColumn<string>(name: "AnalysisWho", table: "Problems", type: "TEXT", maxLength: 2000, nullable: true);
        migrationBuilder.AddColumn<string>(name: "AnalysisWhere", table: "Problems", type: "TEXT", maxLength: 2000, nullable: true);
        migrationBuilder.AddColumn<string>(name: "AnalysisWhen", table: "Problems", type: "TEXT", maxLength: 2000, nullable: true);
        migrationBuilder.AddColumn<string>(name: "AnalysisHow", table: "Problems", type: "TEXT", maxLength: 2000, nullable: true);
        migrationBuilder.AddColumn<int>(name: "ScopeItemId", table: "WorkLogs", type: "INTEGER", nullable: true);

        migrationBuilder.CreateIndex(name: "IX_WorkLogs_ScopeItemId", table: "WorkLogs", column: "ScopeItemId");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(name: "IX_WorkLogs_ScopeItemId", table: "WorkLogs");
        migrationBuilder.DropColumn(name: "ScopeItemId", table: "WorkLogs");
        migrationBuilder.DropColumn(name: "AnalysisWhat", table: "Problems");
        migrationBuilder.DropColumn(name: "AnalysisWhy", table: "Problems");
        migrationBuilder.DropColumn(name: "AnalysisWho", table: "Problems");
        migrationBuilder.DropColumn(name: "AnalysisWhere", table: "Problems");
        migrationBuilder.DropColumn(name: "AnalysisWhen", table: "Problems");
        migrationBuilder.DropColumn(name: "AnalysisHow", table: "Problems");
    }
}
