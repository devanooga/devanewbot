using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace devanewbot.Migrations
{
    /// <inheritdoc />
    public partial class HideModerationActions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "HiddenAt",
                table: "ModerationActions",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "HiddenBy",
                table: "ModerationActions",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "HiddenAt",
                table: "ModerationActions");

            migrationBuilder.DropColumn(
                name: "HiddenBy",
                table: "ModerationActions");
        }
    }
}
