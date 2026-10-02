using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HeroStory.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddPortraitConsentAudit : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ConsentGrantedAt",
                table: "UserPortraits");

            migrationBuilder.DropColumn(
                name: "PortraitConsentGrantedAt",
                table: "GenerationJobs");

            migrationBuilder.AddColumn<Guid>(
                name: "PortraitConsentRecordId",
                table: "GenerationJobs",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "PortraitAuditEvents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SubjectUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ActorType = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    ActorUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    EventType = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    PortraitId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ConsentRecordId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RelatedPortraitId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    SessionId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    SceneId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    GenerationJobId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    DetailCode = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true),
                    OccurredAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PortraitAuditEvents", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PortraitConsentRecords",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PortraitId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Purpose = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    PolicyVersion = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ProviderScope = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    GrantedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PortraitConsentRecords", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PortraitConsentRecords_UserPortraits_PortraitId",
                        column: x => x.PortraitId,
                        principalTable: "UserPortraits",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_GenerationJobs_PortraitConsentRecordId",
                table: "GenerationJobs",
                column: "PortraitConsentRecordId");

            migrationBuilder.CreateIndex(
                name: "IX_PortraitAuditEvents_ConsentRecordId",
                table: "PortraitAuditEvents",
                column: "ConsentRecordId");

            migrationBuilder.CreateIndex(
                name: "IX_PortraitAuditEvents_GenerationJobId",
                table: "PortraitAuditEvents",
                column: "GenerationJobId");

            migrationBuilder.CreateIndex(
                name: "IX_PortraitAuditEvents_SubjectUserId_OccurredAt",
                table: "PortraitAuditEvents",
                columns: new[] { "SubjectUserId", "OccurredAt" });

            migrationBuilder.CreateIndex(
                name: "IX_PortraitConsentRecords_PortraitId",
                table: "PortraitConsentRecords",
                column: "PortraitId");

            migrationBuilder.CreateIndex(
                name: "IX_PortraitConsentRecords_UserId_PortraitId_GrantedAt",
                table: "PortraitConsentRecords",
                columns: new[] { "UserId", "PortraitId", "GrantedAt" });

            migrationBuilder.AddForeignKey(
                name: "FK_GenerationJobs_PortraitConsentRecords_PortraitConsentRecordId",
                table: "GenerationJobs",
                column: "PortraitConsentRecordId",
                principalTable: "PortraitConsentRecords",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_GenerationJobs_PortraitConsentRecords_PortraitConsentRecordId",
                table: "GenerationJobs");

            migrationBuilder.DropTable(
                name: "PortraitAuditEvents");

            migrationBuilder.DropTable(
                name: "PortraitConsentRecords");

            migrationBuilder.DropIndex(
                name: "IX_GenerationJobs_PortraitConsentRecordId",
                table: "GenerationJobs");

            migrationBuilder.DropColumn(
                name: "PortraitConsentRecordId",
                table: "GenerationJobs");

            migrationBuilder.AddColumn<DateTime>(
                name: "ConsentGrantedAt",
                table: "UserPortraits",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "PortraitConsentGrantedAt",
                table: "GenerationJobs",
                type: "datetime2",
                nullable: true);
        }
    }
}
