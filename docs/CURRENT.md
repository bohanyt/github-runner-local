# CURRENT — github-runner-local

Updated: 2026-09-27. Phase: **GRL014_REV2_SOURCE_IMPLEMENTATION_READY; LIVE_ACTIVATION_GATED; GRL015_ACCEPTED**.

## Authority

- Canonical branch: main; Control Tower coordinates on Issue #1.
- Handoff: `docs/control-tower/handoffs/GRL-20260927-REV2-SOL-SOURCE-READY-V51.md`.
- Sentinel: `END_OF_GRL_HANDOFF key=GRL-20260927-REV2-SOL-SOURCE-READY-V51 sections=5`.
- Active task: Issue #18 / GRL-014 revision-2 SOURCE implementation.
- Active packet: Issue #18 comment **5853140232**, through `END_OF_GRL014_SOURCE_PACKET key=GRL014-REV2-SOL-SOURCE-20260927 sections=6`.

## Source baseline, not a live PASS

CT disposition: `GRL014_REV2_ACCEPTED_AS_SOURCE_BASELINE_WITH_CONSTRAINTS`.

Design reference: DRAFT PR #23, head `ccd580aec36432c4b40eeedecf3a04fab17f9a97`, `docs/design/GRL014_SAFE_SWITCHING_PROTOCOL.md`. Revision-2 §0 supersedes revision-1 label/variable/timer fencing. Designer handoff `5853104447`; designer release `5853105794`.

A NEW local Sol implementation worker may take one broad claim after the CT release and ownership check. No further Opus research round is required first. PR #23 stays draft/unmerged; it is not the source implementation PR.

## Implementation

Use a separate worktree from exact fresh main, preferred branch `feat/grl014-job-gate`, and ONE new DRAFT source PR.

Implement S1/S2/S5/S6 (local hooks/gate/ledger/intent/configuration, proof-gated local lifecycle, lint and tests). S3/S4 may be source/fake-tested only behind a disabled capability. Do not wait for OD-G to write this source; do not create a control repo or activate cross-machine behavior.

Packet constraints include actual Worker-exit evidence beyond `.done`, hook files outside the extracted runner directory, recoverable `.env` handling, cross-process handshake tests, pending ownership-publication fences, and stale-artifact/rerun/pre/post-step refusal tests. These constraints are requirements to prove, not evidence already obtained.

Keep the feature disabled by default in normal live composition, with explicit synthetic/test activation. No automatic migration of existing roots. A gate-enabled instance fails closed if its prerequisites are unavailable.

The local goal finishes at SOURCE_IMPLEMENTATION_REVIEW_READY, DRAFT PR, exact-head evidence and claim release. One independent integrated review and CT merge/live gate come later. No self-review or waiting-loop after handoff.

## Preserve completed office work

G1 and GRL-015 remain accepted; do not rerun them without a concrete regression.

- Office identity: `grl-office` ID 3; last witnessed runner version 2.337.0. ID 2 is removed/historical.
- Proven product build source: `1e74ffdbe9d64dc96bb30007c72af80c625b6946`, containing PR #22 merge `34ccb0c999873418a5077bd3da793a460901a968`.
- Private main at accepted evidence: `2c8da8834e785ba012001b9427bd68380401bec7`.
- Operator evidence: `5845412219` and `5852213379`; CT acceptance: `5852242726`.

The office product, launcher, roots and running state are not targets of source development. Keep build outputs separate.

## Gates and safety

Only SOURCE work is open. OD-A/C/D and the live part of OD-F remain pending; OD-G is pending; OD-B is withdrawn. No personal enrollment, control repo, new App permissions, remote takeover or scheduled keep-alive.

No live hook/.env installation, private workflow/label/variable changes, job dispatch/rerun, Stop Now, reboot, service/security/sleep changes, global auth/env reset or GitHub-hosted Actions.

ARCHIVE/CHECKPOINT FIRST. Preserve old checkout/worktrees/notes and existing configuration. No force-push, destructive cleanup or credential exposure.
