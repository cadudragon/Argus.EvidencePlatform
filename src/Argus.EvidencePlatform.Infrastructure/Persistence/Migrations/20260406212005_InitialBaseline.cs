using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Argus.EvidencePlatform.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialBaseline : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "argus");

            migrationBuilder.CreateTable(
                name: "activation_tokens",
                schema: "argus",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Token = table.Column<string>(type: "character varying(9)", maxLength: 9, nullable: false),
                    CaseId = table.Column<Guid>(type: "uuid", nullable: false),
                    CaseExternalId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    IssuedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ValidUntil = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ConsumedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ConsumedByDeviceId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_activation_tokens", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "audit_entries",
                schema: "argus",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CaseId = table.Column<Guid>(type: "uuid", nullable: false),
                    ActorType = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    ActorId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    Action = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    EntityType = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    EntityId = table.Column<Guid>(type: "uuid", nullable: false),
                    OccurredAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CorrelationId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    PayloadJson = table.Column<string>(type: "jsonb", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_audit_entries", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "cases",
                schema: "argus",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    FirebaseAppId = table.Column<Guid>(type: "uuid", nullable: false),
                    ExternalCaseId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    Title = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    Description = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: true),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ClosedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_cases", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "device_sources",
                schema: "argus",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DeviceId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    CaseId = table.Column<Guid>(type: "uuid", nullable: false),
                    CaseExternalId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    EnrolledAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ValidUntil = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    LastSeenAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_device_sources", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "evidence_items",
                schema: "argus",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CaseId = table.Column<Guid>(type: "uuid", nullable: false),
                    SourceId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    EvidenceType = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    CaptureTimestamp = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ReceivedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Classification = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_evidence_items", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "export_jobs",
                schema: "argus",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CaseId = table.Column<Guid>(type: "uuid", nullable: false),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    RequestedBy = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    RequestedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CompletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_export_jobs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "fcm_token_bindings",
                schema: "argus",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    FirebaseAppId = table.Column<Guid>(type: "uuid", nullable: false),
                    DeviceId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    FcmToken = table.Column<string>(type: "character varying(4096)", maxLength: 4096, nullable: false),
                    BoundAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_fcm_token_bindings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "firebase_app_registrations",
                schema: "argus",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Key = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    DisplayName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    ProjectId = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    ServiceAccountPath = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: false),
                    IsActiveForNewCases = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_firebase_app_registrations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "notification_captures",
                schema: "argus",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CaseId = table.Column<Guid>(type: "uuid", nullable: false),
                    CaseExternalId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    DeviceId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    Sha256 = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    CaptureTimestamp = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    PackageName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    Title = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    Text = table.Column<string>(type: "character varying(4096)", maxLength: 4096, nullable: true),
                    BigText = table.Column<string>(type: "character varying(16384)", maxLength: 16384, nullable: true),
                    NotificationTimestamp = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Category = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    ReceivedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_notification_captures", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "text_capture_batches",
                schema: "argus",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CaseId = table.Column<Guid>(type: "uuid", nullable: false),
                    CaseExternalId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    DeviceId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    Sha256 = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    CaptureTimestamp = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CaptureCount = table.Column<int>(type: "integer", nullable: false),
                    PayloadJson = table.Column<string>(type: "jsonb", nullable: false),
                    PackageNamesJson = table.Column<string>(type: "jsonb", nullable: false),
                    ReceivedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_text_capture_batches", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "evidence_blobs",
                schema: "argus",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EvidenceItemId = table.Column<Guid>(type: "uuid", nullable: false),
                    ContainerName = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    BlobName = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: false),
                    BlobVersionId = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    ContentType = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    SizeBytes = table.Column<long>(type: "bigint", nullable: false),
                    Sha256 = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    StoredAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_evidence_blobs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_evidence_blobs_evidence_items_EvidenceItemId",
                        column: x => x.EvidenceItemId,
                        principalSchema: "argus",
                        principalTable: "evidence_items",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_activation_tokens_Token",
                schema: "argus",
                table: "activation_tokens",
                column: "Token",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_cases_ExternalCaseId",
                schema: "argus",
                table: "cases",
                column: "ExternalCaseId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_cases_FirebaseAppId",
                schema: "argus",
                table: "cases",
                column: "FirebaseAppId");

            migrationBuilder.CreateIndex(
                name: "IX_device_sources_DeviceId",
                schema: "argus",
                table: "device_sources",
                column: "DeviceId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_evidence_blobs_EvidenceItemId",
                schema: "argus",
                table: "evidence_blobs",
                column: "EvidenceItemId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_fcm_token_bindings_DeviceId",
                schema: "argus",
                table: "fcm_token_bindings",
                column: "DeviceId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_fcm_token_bindings_FirebaseAppId",
                schema: "argus",
                table: "fcm_token_bindings",
                column: "FirebaseAppId");

            migrationBuilder.CreateIndex(
                name: "IX_firebase_app_registrations_Key",
                schema: "argus",
                table: "firebase_app_registrations",
                column: "Key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_notification_captures_CaseId_CaptureTimestamp",
                schema: "argus",
                table: "notification_captures",
                columns: new[] { "CaseId", "CaptureTimestamp" });

            migrationBuilder.CreateIndex(
                name: "IX_text_capture_batches_CaseId_CaptureTimestamp",
                schema: "argus",
                table: "text_capture_batches",
                columns: new[] { "CaseId", "CaptureTimestamp" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "activation_tokens",
                schema: "argus");

            migrationBuilder.DropTable(
                name: "audit_entries",
                schema: "argus");

            migrationBuilder.DropTable(
                name: "cases",
                schema: "argus");

            migrationBuilder.DropTable(
                name: "device_sources",
                schema: "argus");

            migrationBuilder.DropTable(
                name: "evidence_blobs",
                schema: "argus");

            migrationBuilder.DropTable(
                name: "export_jobs",
                schema: "argus");

            migrationBuilder.DropTable(
                name: "fcm_token_bindings",
                schema: "argus");

            migrationBuilder.DropTable(
                name: "firebase_app_registrations",
                schema: "argus");

            migrationBuilder.DropTable(
                name: "notification_captures",
                schema: "argus");

            migrationBuilder.DropTable(
                name: "text_capture_batches",
                schema: "argus");

            migrationBuilder.DropTable(
                name: "evidence_items",
                schema: "argus");
        }
    }
}
