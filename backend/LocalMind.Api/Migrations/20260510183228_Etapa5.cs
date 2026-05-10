using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LocalMind.Api.Migrations
{
    /// <inheritdoc />
    public partial class Etapa5 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ChatMetrics",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    UserId = table.Column<int>(type: "INTEGER", nullable: false),
                    ConversationId = table.Column<int>(type: "INTEGER", nullable: true),
                    ModelUsed = table.Column<string>(type: "TEXT", nullable: false),
                    ResponseTimeMs = table.Column<long>(type: "INTEGER", nullable: false),
                    ApproxTokens = table.Column<int>(type: "INTEGER", nullable: false),
                    UsedRag = table.Column<bool>(type: "INTEGER", nullable: false),
                    UsedTool = table.Column<bool>(type: "INTEGER", nullable: false),
                    ToolName = table.Column<string>(type: "TEXT", nullable: true),
                    ChunksUsed = table.Column<int>(type: "INTEGER", nullable: false),
                    Route = table.Column<string>(type: "TEXT", nullable: false),
                    Error = table.Column<string>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChatMetrics", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ChatMetrics_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ChatMetrics_UserId_CreatedAt",
                table: "ChatMetrics",
                columns: new[] { "UserId", "CreatedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ChatMetrics");
        }
    }
}
