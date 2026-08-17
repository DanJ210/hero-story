using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HeroStory.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddSceneRevisionLineage : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Scenes_SessionId_SequenceNumber",
                table: "Scenes");

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "Scenes",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ParentSceneId",
                table: "Scenes",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "RevisedFromSceneId",
                table: "Scenes",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Scenes_ParentSceneId",
                table: "Scenes",
                column: "ParentSceneId");

            migrationBuilder.CreateIndex(
                name: "IX_Scenes_RevisedFromSceneId",
                table: "Scenes",
                column: "RevisedFromSceneId");

            migrationBuilder.CreateIndex(
                name: "IX_Scenes_SessionId_SequenceNumber",
                table: "Scenes",
                columns: new[] { "SessionId", "SequenceNumber" },
                unique: true,
                filter: "[IsActive] = 1");

            migrationBuilder.AddForeignKey(
                name: "FK_Scenes_Scenes_ParentSceneId",
                table: "Scenes",
                column: "ParentSceneId",
                principalTable: "Scenes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Scenes_Scenes_RevisedFromSceneId",
                table: "Scenes",
                column: "RevisedFromSceneId",
                principalTable: "Scenes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Scenes_Scenes_ParentSceneId",
                table: "Scenes");

            migrationBuilder.DropForeignKey(
                name: "FK_Scenes_Scenes_RevisedFromSceneId",
                table: "Scenes");

            migrationBuilder.DropIndex(
                name: "IX_Scenes_ParentSceneId",
                table: "Scenes");

            migrationBuilder.DropIndex(
                name: "IX_Scenes_RevisedFromSceneId",
                table: "Scenes");

            migrationBuilder.DropIndex(
                name: "IX_Scenes_SessionId_SequenceNumber",
                table: "Scenes");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "Scenes");

            migrationBuilder.DropColumn(
                name: "ParentSceneId",
                table: "Scenes");

            migrationBuilder.DropColumn(
                name: "RevisedFromSceneId",
                table: "Scenes");

            migrationBuilder.CreateIndex(
                name: "IX_Scenes_SessionId_SequenceNumber",
                table: "Scenes",
                columns: new[] { "SessionId", "SequenceNumber" },
                unique: true);
        }
    }
}
