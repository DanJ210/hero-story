using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HeroStory.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddAutomaticLikenessOptIn : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "LikenessEnabled",
                table: "StorySessions",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LikenessEnabled",
                table: "StorySessions");
        }
    }
}
