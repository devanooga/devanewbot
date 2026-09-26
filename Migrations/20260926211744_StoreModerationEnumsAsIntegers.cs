using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace devanewbot.Migrations
{
    /// <inheritdoc />
    public partial class StoreModerationEnumsAsIntegers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                ALTER TABLE "ModerationActions"
                    ALTER COLUMN "Kind" TYPE integer USING CASE "Kind"
                        WHEN 'RemovedMessage' THEN 0
                        WHEN 'Deactivated' THEN 1
                        WHEN 'ChannelBan' THEN 2
                        WHEN 'ChannelBanLifted' THEN 3
                        ELSE 4
                    END,
                    ALTER COLUMN "Source" TYPE integer USING CASE "Source"
                        WHEN 'Slack' THEN 0
                        WHEN 'Admin' THEN 1
                        ELSE 2
                    END;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                ALTER TABLE "ModerationActions"
                    ALTER COLUMN "Kind" TYPE text USING CASE "Kind"
                        WHEN 0 THEN 'RemovedMessage'
                        WHEN 1 THEN 'Deactivated'
                        WHEN 2 THEN 'ChannelBan'
                        WHEN 3 THEN 'ChannelBanLifted'
                        ELSE 'Other'
                    END,
                    ALTER COLUMN "Source" TYPE text USING CASE "Source"
                        WHEN 0 THEN 'Slack'
                        WHEN 1 THEN 'Admin'
                        ELSE 'Imported'
                    END;
                """);
        }
    }
}
