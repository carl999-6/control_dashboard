using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Dashboard.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class SeoEditorialFoundation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SeoOpportunities",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    ProjectId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Query = table.Column<string>(type: "TEXT", maxLength: 220, nullable: false),
                    TargetPage = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    Evidence = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: false),
                    Hypothesis = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: false),
                    Status = table.Column<string>(type: "TEXT", maxLength: 30, nullable: false),
                    DataSource = table.Column<string>(type: "TEXT", maxLength: 30, nullable: false),
                    BaselineImpressions = table.Column<int>(type: "INTEGER", nullable: false),
                    BaselineClicks = table.Column<int>(type: "INTEGER", nullable: false),
                    IsDemoData = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SeoOpportunities", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SeoOpportunities_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ContentPieces",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    ProjectId = table.Column<Guid>(type: "TEXT", nullable: false),
                    SeoOpportunityId = table.Column<Guid>(type: "TEXT", nullable: true),
                    Title = table.Column<string>(type: "TEXT", maxLength: 220, nullable: false),
                    Slug = table.Column<string>(type: "TEXT", maxLength: 220, nullable: false),
                    ContentType = table.Column<string>(type: "TEXT", maxLength: 30, nullable: false),
                    PrimaryKeyword = table.Column<string>(type: "TEXT", maxLength: 220, nullable: false),
                    SearchIntent = table.Column<string>(type: "TEXT", maxLength: 80, nullable: false),
                    Hypothesis = table.Column<string>(type: "TEXT", maxLength: 1200, nullable: false),
                    BaselineSummary = table.Column<string>(type: "TEXT", maxLength: 1200, nullable: false),
                    Objective = table.Column<string>(type: "TEXT", maxLength: 800, nullable: false),
                    Owner = table.Column<string>(type: "TEXT", maxLength: 120, nullable: false),
                    Brief = table.Column<string>(type: "TEXT", maxLength: 10000, nullable: false),
                    DraftMarkdown = table.Column<string>(type: "TEXT", maxLength: 60000, nullable: false),
                    MetaTitle = table.Column<string>(type: "TEXT", maxLength: 180, nullable: false),
                    MetaDescription = table.Column<string>(type: "TEXT", maxLength: 320, nullable: false),
                    Status = table.Column<string>(type: "TEXT", maxLength: 30, nullable: false),
                    ScheduledFor = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                    MeasuredAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                    ResultImpressions = table.Column<int>(type: "INTEGER", nullable: true),
                    ResultClicks = table.Column<int>(type: "INTEGER", nullable: true),
                    ResultNotes = table.Column<string>(type: "TEXT", maxLength: 1500, nullable: false),
                    SimulatedWordPressUrl = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: false),
                    IsDemoData = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ContentPieces", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ContentPieces_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ContentPieces_SeoOpportunities_SeoOpportunityId",
                        column: x => x.SeoOpportunityId,
                        principalTable: "SeoOpportunities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "EditorialHistory",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    ProjectId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ContentPieceId = table.Column<Guid>(type: "TEXT", nullable: false),
                    FromStatus = table.Column<string>(type: "TEXT", maxLength: 30, nullable: false),
                    ToStatus = table.Column<string>(type: "TEXT", maxLength: 30, nullable: false),
                    Note = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: false),
                    Actor = table.Column<string>(type: "TEXT", maxLength: 120, nullable: false),
                    ChangedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EditorialHistory", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EditorialHistory_ContentPieces_ContentPieceId",
                        column: x => x.ContentPieceId,
                        principalTable: "ContentPieces",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_EditorialHistory_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ContentPieces_ProjectId_ScheduledFor",
                table: "ContentPieces",
                columns: new[] { "ProjectId", "ScheduledFor" });

            migrationBuilder.CreateIndex(
                name: "IX_ContentPieces_ProjectId_Status",
                table: "ContentPieces",
                columns: new[] { "ProjectId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_ContentPieces_SeoOpportunityId",
                table: "ContentPieces",
                column: "SeoOpportunityId");

            migrationBuilder.CreateIndex(
                name: "IX_EditorialHistory_ContentPieceId",
                table: "EditorialHistory",
                column: "ContentPieceId");

            migrationBuilder.CreateIndex(
                name: "IX_EditorialHistory_ProjectId_ContentPieceId_ChangedAt",
                table: "EditorialHistory",
                columns: new[] { "ProjectId", "ContentPieceId", "ChangedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_SeoOpportunities_ProjectId_Status",
                table: "SeoOpportunities",
                columns: new[] { "ProjectId", "Status" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EditorialHistory");

            migrationBuilder.DropTable(
                name: "ContentPieces");

            migrationBuilder.DropTable(
                name: "SeoOpportunities");
        }
    }
}
