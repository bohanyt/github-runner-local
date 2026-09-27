# CURRENT — github-runner-local

Updated: 2026-09-27. Phase: **GRL014_SOURCE_MERGED; A1_PROOF_PENDING; A5_ODG_GATED; LIVE_ACTIVATION_GATED; GRL015_ACCEPTED**.

## Authority

- Canonical branch: main; Control Tower coordinates on Issue #1.
- Handoff: `docs/control-tower/handoffs/GRL-20260927-SOURCE-MERGED-A1-NEXT-V57.md`.
- Sentinel: `END_OF_GRL_HANDOFF key=GRL-20260927-SOURCE-MERGED-A1-NEXT-V57 sections=5`.
- Source acceptance: Issue #18 comment **`5855825625`**, FULL through `END_OF_GRL014_SOURCE_ACCEPTANCE key=GRL014-PR24-MERGED-20260927 sections=5`.

## Merged source

PR #24 exact reviewed head `90df67d1d9bdf7b1987fcaee27056510bd2240e4` merged at
`25b1ecf60baa9fc9ef132348f9e771f44b8cb7b5`.

Independent source review chain is complete. F1/F2/F3/F4 are accepted closed; no further source correction loop absent a new regression.

PR #23 remains an unmerged draft design reference at `ccd580aec36432c4b40eeedecf3a04fab17f9a97`.

## Remaining proof

A1 PENDING / DEFERRED_LIVE_PROOF: actual pinned Windows Worker inheritance of runner-root `.env` hook variables and real hook refusal ordering.

A5 PENDING / DEFERRED_LIVE_PROOF: live Git-ref CAS behavior; also gated by pending OD-G.

Normal live composition and cross-machine capability remain disabled. No existing root is auto-migrated.

## Next

**SEQUENTIAL:** smallest honest A1 witness -> CT proof adjudication -> later live gate. A5 waits for explicit OD-G.

Prefer isolated pinned-runner A1 proof that genuinely exercises Listener -> Worker inheritance. If that cannot prove A1, office ID 3 requires a separate owner-authorized checkpointed witness before any root/.env/hook/restart/job change.

## Safety

G1/GRL-015 remain accepted. Office ID 3 stays untouched.
No control repo/App permission/personal enrollment/remote takeover/scheduled keep-alive.
No hosted Actions, service/autostart/security/sleep changes, global auth/env reset, destructive cleanup or credential exposure.
ARCHIVE/CHECKPOINT FIRST.