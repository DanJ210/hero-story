using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HeroStory.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AllowMultipleGenerationJobsPerScene : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_GenerationJobs_SceneId",
                table: "GenerationJobs");

            migrationBuilder.CreateIndex(
                name: "IX_GenerationJobs_SceneId",
                table: "GenerationJobs",
                column: "SceneId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_GenerationJobs_SceneId",
                table: "GenerationJobs");

            migrationBuilder.CreateIndex(
                name: "IX_GenerationJobs_SceneId",
                table: "GenerationJobs",
                column: "SceneId",
                unique: true);
        }
    }
}
