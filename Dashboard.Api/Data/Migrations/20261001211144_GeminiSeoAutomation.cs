using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Dashboard.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class GeminiSeoAutomation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AiAutomationSettings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    ProjectId = table.Column<Guid>(type: "TEXT", nullable: false),
                    IsEnabled = table.Column<bool>(type: "INTEGER", nullable: false),
                    Model = table.Column<string>(type: "TEXT", maxLength: 120, nullable: false),
                    MinimumImpressions = table.Column<int>(type: "INTEGER", nullable: false),
                    MinimumPosition = table.Column<double>(type: "REAL", nullable: false),
                    MaximumPosition = table.Column<double>(type: "REAL", nullable: false),
                    MaximumCtr = table.Column<double>(type: "REAL", nullable: false),
                    MaximumSeoDraftsPerDay = table.Column<int>(type: "INTEGER", nullable: false),
                    MinimumDraftWords = table.Column<int>(type: "INTEGER", nullable: false),
                    MaximumOutputTokens = table.Column<int>(type: "INTEGER", nullable: false),
                    Owner = table.Column<string>(type: "TEXT", maxLength: 120, nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AiAutomationSettings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AiAutomationSettings_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AiAutomationSettings_ProjectId",
                table: "AiAutomationSettings",
                column: "ProjectId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AiAutomationSettings");
        }
    }
}
