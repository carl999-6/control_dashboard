using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Dashboard.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class SearchConsoleReadOnly : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "IntegrationConnections",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    ProjectId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Provider = table.Column<string>(type: "TEXT", maxLength: 80, nullable: false),
                    Status = table.Column<string>(type: "TEXT", maxLength: 30, nullable: false),
                    ExternalAccount = table.Column<string>(type: "TEXT", maxLength: 160, nullable: false),
                    ResourceId = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    EncryptedRefreshToken = table.Column<string>(type: "TEXT", maxLength: 4000, nullable: false),
                    GrantedScopes = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: false),
                    LookbackDays = table.Column<int>(type: "INTEGER", nullable: false),
                    RowLimit = table.Column<int>(type: "INTEGER", nullable: false),
                    LastSyncAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                    LastError = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IntegrationConnections", x => x.Id);
                    table.ForeignKey(
                        name: "FK_IntegrationConnections_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "OAuthStates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    ProjectId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Provider = table.Column<string>(type: "TEXT", maxLength: 80, nullable: false),
                    StateHash = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    ExpiresAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    UsedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OAuthStates", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OAuthStates_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SearchConsoleMetrics",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    ProjectId = table.Column<Guid>(type: "TEXT", nullable: false),
                    IntegrationConnectionId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Date = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    Query = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: false),
                    Page = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: false),
                    Clicks = table.Column<double>(type: "REAL", nullable: false),
                    Impressions = table.Column<double>(type: "REAL", nullable: false),
                    Ctr = table.Column<double>(type: "REAL", nullable: false),
                    Position = table.Column<double>(type: "REAL", nullable: false),
                    SyncedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SearchConsoleMetrics", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SearchConsoleMetrics_IntegrationConnections_IntegrationConnectionId",
                        column: x => x.IntegrationConnectionId,
                        principalTable: "IntegrationConnections",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SearchConsoleMetrics_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_IntegrationConnections_ProjectId_Provider",
                table: "IntegrationConnections",
                columns: new[] { "ProjectId", "Provider" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OAuthStates_ProjectId",
                table: "OAuthStates",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_OAuthStates_StateHash",
                table: "OAuthStates",
                column: "StateHash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SearchConsoleMetrics_IntegrationConnectionId_Date",
                table: "SearchConsoleMetrics",
                columns: new[] { "IntegrationConnectionId", "Date" });

            migrationBuilder.CreateIndex(
                name: "IX_SearchConsoleMetrics_ProjectId_Date",
                table: "SearchConsoleMetrics",
                columns: new[] { "ProjectId", "Date" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "OAuthStates");

            migrationBuilder.DropTable(
                name: "SearchConsoleMetrics");

            migrationBuilder.DropTable(
                name: "IntegrationConnections");
        }
    }
}
