using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Dashboard.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class PostHogAnalytics : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PostHogConnections",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    ProjectId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Region = table.Column<string>(type: "TEXT", maxLength: 2, nullable: false),
                    ExternalProjectId = table.Column<int>(type: "INTEGER", nullable: false),
                    PublicToken = table.Column<string>(type: "TEXT", maxLength: 160, nullable: false),
                    ApiKeyEnvironmentVariable = table.Column<string>(type: "TEXT", maxLength: 120, nullable: false),
                    LookbackDays = table.Column<int>(type: "INTEGER", nullable: false),
                    RowLimit = table.Column<int>(type: "INTEGER", nullable: false),
                    IsEnabled = table.Column<bool>(type: "INTEGER", nullable: false),
                    LastSyncAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                    LastError = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PostHogConnections", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PostHogConnections_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PostHogMetrics",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    ProjectId = table.Column<Guid>(type: "TEXT", nullable: false),
                    PostHogConnectionId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Date = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    Event = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                    Source = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Medium = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Campaign = table.Column<string>(type: "TEXT", maxLength: 140, nullable: false),
                    Content = table.Column<string>(type: "TEXT", maxLength: 160, nullable: false),
                    Path = table.Column<string>(type: "TEXT", maxLength: 300, nullable: false),
                    EventCount = table.Column<int>(type: "INTEGER", nullable: false),
                    Sessions = table.Column<int>(type: "INTEGER", nullable: false),
                    SyncedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PostHogMetrics", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PostHogMetrics_PostHogConnections_PostHogConnectionId",
                        column: x => x.PostHogConnectionId,
                        principalTable: "PostHogConnections",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PostHogMetrics_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PostHogConnections_ProjectId",
                table: "PostHogConnections",
                column: "ProjectId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PostHogMetrics_PostHogConnectionId",
                table: "PostHogMetrics",
                column: "PostHogConnectionId");

            migrationBuilder.CreateIndex(
                name: "IX_PostHogMetrics_ProjectId_Date",
                table: "PostHogMetrics",
                columns: new[] { "ProjectId", "Date" });

            migrationBuilder.CreateIndex(
                name: "IX_PostHogMetrics_ProjectId_Date_Event_Source_Medium_Campaign_Content_Path",
                table: "PostHogMetrics",
                columns: new[] { "ProjectId", "Date", "Event", "Source", "Medium", "Campaign", "Content", "Path" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PostHogMetrics");

            migrationBuilder.DropTable(
                name: "PostHogConnections");
        }
    }
}
