using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Dashboard.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class ExecutionControls : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ApiRatePlans",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    ProjectId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Provider = table.Column<string>(type: "TEXT", maxLength: 80, nullable: false),
                    Model = table.Column<string>(type: "TEXT", maxLength: 120, nullable: false),
                    InputUsdPerMillion = table.Column<decimal>(type: "TEXT", precision: 18, scale: 8, nullable: false),
                    OutputUsdPerMillion = table.Column<decimal>(type: "TEXT", precision: 18, scale: 8, nullable: false),
                    EffectiveFrom = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsDemoData = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ApiRatePlans", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ApiRatePlans_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ProjectBudgets",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    ProjectId = table.Column<Guid>(type: "TEXT", nullable: false),
                    DailyLimitUsd = table.Column<decimal>(type: "TEXT", precision: 18, scale: 6, nullable: false),
                    MonthlyLimitUsd = table.Column<decimal>(type: "TEXT", precision: 18, scale: 6, nullable: false),
                    WarningPercent = table.Column<int>(type: "INTEGER", nullable: false),
                    ExchangeRateGtqPerUsd = table.Column<decimal>(type: "TEXT", precision: 18, scale: 6, nullable: false),
                    IsPaused = table.Column<bool>(type: "INTEGER", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProjectBudgets", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProjectBudgets_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Executions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    ProjectId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ApiRatePlanId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ParentExecutionId = table.Column<Guid>(type: "TEXT", nullable: true),
                    IdempotencyKey = table.Column<string>(type: "TEXT", maxLength: 160, nullable: false),
                    Provider = table.Column<string>(type: "TEXT", maxLength: 80, nullable: false),
                    Model = table.Column<string>(type: "TEXT", maxLength: 120, nullable: false),
                    Flow = table.Column<string>(type: "TEXT", maxLength: 120, nullable: false),
                    Status = table.Column<string>(type: "TEXT", maxLength: 30, nullable: false),
                    ApprovalRequired = table.Column<bool>(type: "INTEGER", nullable: false),
                    ApprovedBy = table.Column<string>(type: "TEXT", maxLength: 120, nullable: false),
                    ApprovedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                    InputUnits = table.Column<int>(type: "INTEGER", nullable: false),
                    OutputUnits = table.Column<int>(type: "INTEGER", nullable: false),
                    EstimatedCostUsd = table.Column<decimal>(type: "TEXT", precision: 18, scale: 8, nullable: false),
                    EstimatedCostGtq = table.Column<decimal>(type: "TEXT", precision: 18, scale: 6, nullable: false),
                    AttemptNumber = table.Column<int>(type: "INTEGER", nullable: false),
                    ErrorCode = table.Column<string>(type: "TEXT", maxLength: 80, nullable: false),
                    ErrorMessage = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: false),
                    IsDemoData = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    StartedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                    CompletedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Executions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Executions_ApiRatePlans_ApiRatePlanId",
                        column: x => x.ApiRatePlanId,
                        principalTable: "ApiRatePlans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Executions_Executions_ParentExecutionId",
                        column: x => x.ParentExecutionId,
                        principalTable: "Executions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Executions_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ExecutionAudit",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    ProjectId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ExecutionRecordId = table.Column<Guid>(type: "TEXT", nullable: false),
                    EventType = table.Column<string>(type: "TEXT", maxLength: 60, nullable: false),
                    FromStatus = table.Column<string>(type: "TEXT", maxLength: 30, nullable: false),
                    ToStatus = table.Column<string>(type: "TEXT", maxLength: 30, nullable: false),
                    Note = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: false),
                    Actor = table.Column<string>(type: "TEXT", maxLength: 120, nullable: false),
                    OccurredAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExecutionAudit", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ExecutionAudit_Executions_ExecutionRecordId",
                        column: x => x.ExecutionRecordId,
                        principalTable: "Executions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ExecutionAudit_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ApiRatePlans_ProjectId_Provider_Model_EffectiveFrom",
                table: "ApiRatePlans",
                columns: new[] { "ProjectId", "Provider", "Model", "EffectiveFrom" });

            migrationBuilder.CreateIndex(
                name: "IX_ExecutionAudit_ExecutionRecordId",
                table: "ExecutionAudit",
                column: "ExecutionRecordId");

            migrationBuilder.CreateIndex(
                name: "IX_ExecutionAudit_ProjectId_ExecutionRecordId_OccurredAt",
                table: "ExecutionAudit",
                columns: new[] { "ProjectId", "ExecutionRecordId", "OccurredAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Executions_ApiRatePlanId",
                table: "Executions",
                column: "ApiRatePlanId");

            migrationBuilder.CreateIndex(
                name: "IX_Executions_ParentExecutionId",
                table: "Executions",
                column: "ParentExecutionId");

            migrationBuilder.CreateIndex(
                name: "IX_Executions_ProjectId_CreatedAt",
                table: "Executions",
                columns: new[] { "ProjectId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Executions_ProjectId_IdempotencyKey",
                table: "Executions",
                columns: new[] { "ProjectId", "IdempotencyKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Executions_ProjectId_Status",
                table: "Executions",
                columns: new[] { "ProjectId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_ProjectBudgets_ProjectId",
                table: "ProjectBudgets",
                column: "ProjectId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ExecutionAudit");

            migrationBuilder.DropTable(
                name: "ProjectBudgets");

            migrationBuilder.DropTable(
                name: "Executions");

            migrationBuilder.DropTable(
                name: "ApiRatePlans");
        }
    }
}
