using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HeroStory.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddStructuredStoryTurn : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ActiveConflict",
                table: "Scenes",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "IsEpisodeComplete",
                table: "Scenes",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "Location",
                table: "Scenes",
                type: "nvarchar(300)",
                maxLength: 300,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "SceneSummary",
                table: "Scenes",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "StoryBeat",
                table: "Scenes",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "StoryStateJson",
                table: "Scenes",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "{}");

            migrationBuilder.AddColumn<int>(
                name: "StoryStateSchemaVersion",
                table: "Scenes",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<string>(
                name: "SuggestedActionsJson",
                table: "Scenes",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: false,
                defaultValue: "[]");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ActiveConflict",
                table: "Scenes");

            migrationBuilder.DropColumn(
                name: "IsEpisodeComplete",
                table: "Scenes");

            migrationBuilder.DropColumn(
                name: "Location",
                table: "Scenes");

            migrationBuilder.DropColumn(
                name: "SceneSummary",
                table: "Scenes");

            migrationBuilder.DropColumn(
                name: "StoryBeat",
                table: "Scenes");

            migrationBuilder.DropColumn(
                name: "StoryStateJson",
                table: "Scenes");

            migrationBuilder.DropColumn(
                name: "StoryStateSchemaVersion",
                table: "Scenes");

            migrationBuilder.DropColumn(
                name: "SuggestedActionsJson",
                table: "Scenes");
        }
    }
}
