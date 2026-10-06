using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Dashboard.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class XAssistantHumanReview : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "MaximumTotalGeminiRunsPerDay",
                table: "AiAutomationSettings",
                type: "INTEGER",
                nullable: false,
                defaultValue: 2);

            migrationBuilder.AddColumn<int>(
                name: "MaximumXProposalsPerDay",
                table: "AiAutomationSettings",
                type: "INTEGER",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.CreateTable(
                name: "XAssistantSettings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    ProjectId = table.Column<Guid>(type: "TEXT", nullable: false),
                    IsEnabled = table.Column<bool>(type: "INTEGER", nullable: false),
                    ApiReadEnabled = table.Column<bool>(type: "INTEGER", nullable: false),
                    SearchQuery = table.Column<string>(type: "TEXT", maxLength: 512, nullable: false),
                    Language = table.Column<string>(type: "TEXT", maxLength: 10, nullable: false),
                    MaximumPostsPerSync = table.Column<int>(type: "INTEGER", nullable: false),
                    ReadCostUsdPerPost = table.Column<decimal>(type: "TEXT", precision: 18, scale: 8, nullable: false),
                    ToneInstructions = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: false),
                    LandingPath = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    UtmCampaign = table.Column<string>(type: "TEXT", maxLength: 120, nullable: false),
                    LastSyncAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                    LastError = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_XAssistantSettings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_XAssistantSettings_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "XSourcePosts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    ProjectId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ExternalPostId = table.Column<string>(type: "TEXT", maxLength: 80, nullable: false),
                    Url = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: false),
                    AuthorUsername = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Text = table.Column<string>(type: "TEXT", maxLength: 10000, nullable: false),
                    Language = table.Column<string>(type: "TEXT", maxLength: 10, nullable: false),
                    LikeCount = table.Column<int>(type: "INTEGER", nullable: false),
                    ReplyCount = table.Column<int>(type: "INTEGER", nullable: false),
                    RepostCount = table.Column<int>(type: "INTEGER", nullable: false),
                    QuoteCount = table.Column<int>(type: "INTEGER", nullable: false),
                    ImpressionCount = table.Column<int>(type: "INTEGER", nullable: false),
                    Status = table.Column<string>(type: "TEXT", maxLength: 30, nullable: false),
                    DataSource = table.Column<string>(type: "TEXT", maxLength: 30, nullable: false),
                    PostedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    ImportedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_XSourcePosts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_XSourcePosts_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "XReplyProposals",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    ProjectId = table.Column<Guid>(type: "TEXT", nullable: false),
                    XSourcePostId = table.Column<Guid>(type: "TEXT", nullable: false),
                    RecommendedReply = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    AlternativeOne = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    AlternativeTwo = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    SelectedReply = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    Rationale = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: false),
                    RiskNotes = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: false),
                    Status = table.Column<string>(type: "TEXT", maxLength: 30, nullable: false),
                    PublishedReplyUrl = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: false),
                    TrackingUrl = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: false),
                    PublishedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_XReplyProposals", x => x.Id);
                    table.ForeignKey(
                        name: "FK_XReplyProposals_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_XReplyProposals_XSourcePosts_XSourcePostId",
                        column: x => x.XSourcePostId,
                        principalTable: "XSourcePosts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_XAssistantSettings_ProjectId",
                table: "XAssistantSettings",
                column: "ProjectId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_XReplyProposals_ProjectId_Status_CreatedAt",
                table: "XReplyProposals",
                columns: new[] { "ProjectId", "Status", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_XReplyProposals_XSourcePostId",
                table: "XReplyProposals",
                column: "XSourcePostId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_XSourcePosts_ProjectId_ExternalPostId",
                table: "XSourcePosts",
                columns: new[] { "ProjectId", "ExternalPostId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_XSourcePosts_ProjectId_Status_PostedAt",
                table: "XSourcePosts",
                columns: new[] { "ProjectId", "Status", "PostedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "XAssistantSettings");

            migrationBuilder.DropTable(
                name: "XReplyProposals");

            migrationBuilder.DropTable(
                name: "XSourcePosts");

            migrationBuilder.DropColumn(
                name: "MaximumTotalGeminiRunsPerDay",
                table: "AiAutomationSettings");

            migrationBuilder.DropColumn(
                name: "MaximumXProposalsPerDay",
                table: "AiAutomationSettings");
        }
    }
}
