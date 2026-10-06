using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Dashboard.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class WordPressDraftConnector : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "WordPressDraftCreatedAt",
                table: "ContentPieces",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "WordPressPostId",
                table: "ContentPieces",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "WordPressStatus",
                table: "ContentPieces",
                type: "TEXT",
                maxLength: 30,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_ContentPieces_ProjectId_WordPressPostId",
                table: "ContentPieces",
                columns: new[] { "ProjectId", "WordPressPostId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ContentPieces_ProjectId_WordPressPostId",
                table: "ContentPieces");

            migrationBuilder.DropColumn(
                name: "WordPressDraftCreatedAt",
                table: "ContentPieces");

            migrationBuilder.DropColumn(
                name: "WordPressPostId",
                table: "ContentPieces");

            migrationBuilder.DropColumn(
                name: "WordPressStatus",
                table: "ContentPieces");
        }
    }
}
