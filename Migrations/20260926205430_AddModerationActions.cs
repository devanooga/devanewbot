using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace devanewbot.Migrations
{
    /// <inheritdoc />
    public partial class AddModerationActions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ModerationActions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OccurredAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Kind = table.Column<string>(type: "text", nullable: false),
                    Source = table.Column<string>(type: "text", nullable: false),
                    Action = table.Column<string>(type: "text", nullable: false),
                    Reason = table.Column<string>(type: "text", nullable: false),
                    Administrator = table.Column<string>(type: "text", nullable: false),
                    AdministratorSlackUserId = table.Column<string>(type: "text", nullable: true),
                    TargetSlackUserId = table.Column<string>(type: "text", nullable: true),
                    ChannelId = table.Column<string>(type: "text", nullable: true),
                    RemovedMessageText = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ModerationActions", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ModerationActions_OccurredAt",
                table: "ModerationActions",
                column: "OccurredAt");

            migrationBuilder.CreateIndex(
                name: "IX_ModerationActions_TargetSlackUserId",
                table: "ModerationActions",
                column: "TargetSlackUserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ModerationActions");
        }
    }
}
