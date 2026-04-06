using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Argus.EvidencePlatform.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ReconcileLegacySchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                alter table if exists argus.cases
                    add column if not exists "FirebaseAppId" uuid;

                alter table if exists argus.fcm_token_bindings
                    add column if not exists "FirebaseAppId" uuid;

                do $$
                declare
                    firebase_app_id uuid;
                    firebase_app_count integer;
                begin
                    if exists (
                        select 1
                        from information_schema.tables
                        where table_schema = 'argus'
                          and table_name = 'firebase_app_registrations') then
                        select count(*)
                        into firebase_app_count
                        from argus.firebase_app_registrations;

                        if firebase_app_count = 1 then
                            select "Id"
                            into firebase_app_id
                            from argus.firebase_app_registrations
                            limit 1;

                            update argus.cases
                            set "FirebaseAppId" = firebase_app_id
                            where "FirebaseAppId" is null
                               or "FirebaseAppId" = '00000000-0000-0000-0000-000000000000';

                            update argus.fcm_token_bindings
                            set "FirebaseAppId" = firebase_app_id
                            where "FirebaseAppId" is null
                               or "FirebaseAppId" = '00000000-0000-0000-0000-000000000000';
                        end if;
                    end if;
                end $$;

                do $$
                begin
                    if exists (
                        select 1
                        from information_schema.columns
                        where table_schema = 'argus'
                          and table_name = 'cases'
                          and column_name = 'FirebaseAppId') then
                        if exists (
                            select 1
                            from argus.cases
                            where "FirebaseAppId" is null
                               or "FirebaseAppId" = '00000000-0000-0000-0000-000000000000') then
                            raise exception 'Cannot reconcile argus.cases.FirebaseAppId because legacy rows still have null or empty values.';
                        end if;

                        alter table argus.cases
                            alter column "FirebaseAppId" set not null;
                    end if;
                end $$;

                do $$
                begin
                    if exists (
                        select 1
                        from information_schema.columns
                        where table_schema = 'argus'
                          and table_name = 'fcm_token_bindings'
                          and column_name = 'FirebaseAppId') then
                        if exists (
                            select 1
                            from argus.fcm_token_bindings
                            where "FirebaseAppId" is null
                               or "FirebaseAppId" = '00000000-0000-0000-0000-000000000000') then
                            raise exception 'Cannot reconcile argus.fcm_token_bindings.FirebaseAppId because legacy rows still have null or empty values.';
                        end if;

                        alter table argus.fcm_token_bindings
                            alter column "FirebaseAppId" set not null;
                    end if;
                end $$;

                alter table if exists argus.evidence_blobs
                    drop column if exists "ImmutabilityState",
                    drop column if exists "LegalHoldState";

                alter table if exists argus.export_jobs
                    drop column if exists "ManifestBlobName",
                    drop column if exists "PackageBlobName";

                create unique index if not exists "IX_activation_tokens_Token"
                    on argus.activation_tokens ("Token");

                create unique index if not exists "IX_cases_ExternalCaseId"
                    on argus.cases ("ExternalCaseId");

                create index if not exists "IX_cases_FirebaseAppId"
                    on argus.cases ("FirebaseAppId");

                create unique index if not exists "IX_device_sources_DeviceId"
                    on argus.device_sources ("DeviceId");

                create unique index if not exists "IX_evidence_blobs_EvidenceItemId"
                    on argus.evidence_blobs ("EvidenceItemId");

                create unique index if not exists "IX_fcm_token_bindings_DeviceId"
                    on argus.fcm_token_bindings ("DeviceId");

                create index if not exists "IX_fcm_token_bindings_FirebaseAppId"
                    on argus.fcm_token_bindings ("FirebaseAppId");

                create unique index if not exists "IX_firebase_app_registrations_Key"
                    on argus.firebase_app_registrations ("Key");

                create index if not exists "IX_notification_captures_CaseId_CaptureTimestamp"
                    on argus.notification_captures ("CaseId", "CaptureTimestamp");

                create index if not exists "IX_text_capture_batches_CaseId_CaptureTimestamp"
                    on argus.text_capture_batches ("CaseId", "CaptureTimestamp");
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
