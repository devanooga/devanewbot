using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace devanewbot.Migrations
{
    /// <inheritdoc />
    public partial class AddInvites : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Invites",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Email = table.Column<string>(type: "text", nullable: false),
                    Ip = table.Column<string>(type: "text", nullable: false),
                    Source = table.Column<string>(type: "text", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    Flag = table.Column<string>(type: "text", nullable: true),
                    FlagMessage = table.Column<string>(type: "text", nullable: true),
                    City = table.Column<string>(type: "text", nullable: true),
                    Region = table.Column<string>(type: "text", nullable: true),
                    Country = table.Column<string>(type: "text", nullable: true),
                    Isp = table.Column<string>(type: "text", nullable: true),
                    Proxy = table.Column<bool>(type: "boolean", nullable: false),
                    Hosting = table.Column<bool>(type: "boolean", nullable: false),
                    Mobile = table.Column<bool>(type: "boolean", nullable: false),
                    LocationJson = table.Column<string>(type: "jsonb", nullable: true),
                    SlackChannelId = table.Column<string>(type: "text", nullable: true),
                    SlackMessageTs = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DecidedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DecidedBy = table.Column<string>(type: "text", nullable: true),
                    DecidedBySlackUserId = table.Column<string>(type: "text", nullable: true),
                    DecisionSource = table.Column<string>(type: "text", nullable: true),
                    Error = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Invites", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Invites_CreatedAt",
                table: "Invites",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Invites_Email",
                table: "Invites",
                column: "Email");

            migrationBuilder.CreateIndex(
                name: "IX_Invites_Status",
                table: "Invites",
                column: "Status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Invites");
        }
    }
}
