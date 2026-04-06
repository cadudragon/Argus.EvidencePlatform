# V1 Functional Scope

## Status

The project is currently running in `functional mode` for local backend and Android validation.

This means:
- feature completeness for local testing is the active priority;
- compliance-heavy capabilities are intentionally deferred;
- integrity and traceability basics remain mandatory from day one;
- this mode is not a production authorization.

## Mandatory In V1 Functional Mode

The following are mandatory and must not be removed while compliance work is on hold:

- stable `Guid` identifiers for cases, evidence items, export jobs, and audit entries;
- `UTC` timestamps for persisted operational events;
- `sha256` capture and persistence for staged and preserved evidence blobs;
- domain invariants that protect data integrity;
- append-style audit entries for core actions such as case creation, evidence preservation, and export queueing;
- separate behavior for `Development` and `Testing` versus production-oriented environments;
- abstraction boundaries for storage, export processing, auth, and time;
- automated tests for domain, validators, and handlers at the current slice-by-slice standard;
- integration tests for the local HTTP flow;
- local-only execution posture unless an explicit production hardening track is approved.

## Deferred For Compliance

The following are intentionally deferred while the team validates functional behavior:

- legal hold workflows;
- retention schedules and evidence expiry rules;
- formal disposition and purge workflows;
- immutable/WORM storage policies;
- cloud KMS/HSM integration and key rotation policy;
- private networking and production secret distribution;
- fine-grained RBAC and case-scoped authorization policies;
- forensic export package completion with final manifest and checksum set for delivery;
- production backup, restore, and disaster recovery controls;
- formal audit review/reporting requirements for external oversight.

These items are deferred as future capabilities, not active runtime behavior.
They should not remain as placeholder fields in public contracts or domain models unless an approved near-term BB needs them explicitly.

## Guardrails While Compliance Is Deferred

Deferring compliance does not authorize weakening the data model or deleting extension points.

The following guardrails apply:

- do not remove hashing, timestamps, IDs, or audit records already in the model;
- do not hard-code local-only assumptions into domain objects;
- do not collapse abstractions that will be needed for cloud storage, export workers, or auth;
- do not treat `functional mode` as production-ready;
- do not add new slices that depend on compliance shortcuts being permanent.

## Exit Criteria For Reopening Compliance Work

Compliance work should move back to active development when all of the following are true:

- local backend slices required for operator workflows are green;
- Android app validation against the local backend is green;
- Firebase-dependent validation flow is green;
- the team is ready to freeze core workflows and harden deployment assumptions;
- export processing moves from queued metadata to actual package generation.

## Current Slice Scope

As of this checkpoint, the active functional slices are:

- `Cases/CreateCase`
- `Cases/GetCase`
- `Evidence/IngestArtifact`
- `Evidence/GetTimeline`
- `Exports/CreateCaseExport`
- `Exports/GetExportJob`

The next remaining scaffolded slice is:

- `Audit/GetCaseAuditTrail`

## Operational Note

Any future documentation or implementation that is compliance-oriented should be explicitly marked as:

- `Deferred for compliance`, or
- `Mandatory in v1 functional mode`

This avoids mixing current test goals with production hardening work.
