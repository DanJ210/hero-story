using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HeroStory.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddPortraitProvenanceToGenerationJobs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "PortraitConsentGrantedAt",
                table: "GenerationJobs",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "PortraitId",
                table: "GenerationJobs",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_GenerationJobs_PortraitId",
                table: "GenerationJobs",
                column: "PortraitId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_GenerationJobs_PortraitId",
                table: "GenerationJobs");

            migrationBuilder.DropColumn(
                name: "PortraitConsentGrantedAt",
                table: "GenerationJobs");

            migrationBuilder.DropColumn(
                name: "PortraitId",
                table: "GenerationJobs");
        }
    }
}
