using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HeroStory.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddContinuitySummary : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ContinuitySummary",
                table: "StorySessions",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "ContinuitySummaryThroughSequence",
                table: "StorySessions",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "ContinuitySummaryUpdatedAt",
                table: "StorySessions",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ContinuitySummary",
                table: "StorySessions");

            migrationBuilder.DropColumn(
                name: "ContinuitySummaryThroughSequence",
                table: "StorySessions");

            migrationBuilder.DropColumn(
                name: "ContinuitySummaryUpdatedAt",
                table: "StorySessions");
        }
    }
}
