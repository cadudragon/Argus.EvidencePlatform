using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Argus.EvidencePlatform.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddEncryptedFcmCommandEnvelope : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "FcmCommandKeyAlg",
                schema: "argus",
                table: "fcm_token_bindings",
                type: "character varying(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "FcmCommandKeyKid",
                schema: "argus",
                table: "fcm_token_bindings",
                type: "character varying(128)",
                maxLength: 128,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "FcmCommandKeyPublicKey",
                schema: "argus",
                table: "fcm_token_bindings",
                type: "character varying(2048)",
                maxLength: 2048,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "case_command_policies",
                schema: "argus",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CaseId = table.Column<Guid>(type: "uuid", nullable: false),
                    StreamStartFps = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_case_command_policies", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_case_command_policies_CaseId",
                schema: "argus",
                table: "case_command_policies",
                column: "CaseId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "case_command_policies",
                schema: "argus");

            migrationBuilder.DropColumn(
                name: "FcmCommandKeyAlg",
                schema: "argus",
                table: "fcm_token_bindings");

            migrationBuilder.DropColumn(
                name: "FcmCommandKeyKid",
                schema: "argus",
                table: "fcm_token_bindings");

            migrationBuilder.DropColumn(
                name: "FcmCommandKeyPublicKey",
                schema: "argus",
                table: "fcm_token_bindings");
        }
    }
}
