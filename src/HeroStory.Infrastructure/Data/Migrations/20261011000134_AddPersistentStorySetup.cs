using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HeroStory.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddPersistentStorySetup : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "StorySetup",
                table: "StorySessions",
                type: "nvarchar(4000)",
                maxLength: 4000,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "StorySetup",
                table: "StorySessions");
        }
    }
}
