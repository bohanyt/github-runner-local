# CURRENT — github-runner-local

Updated: 2026-09-24. Phase: **C_IMPLEMENTED / AWAITING_INDEPENDENT_EXACT_HEAD_REVIEW + E_LOCAL_TAKEOVER_READY**.

## Authority

- Canonical branch: `main`.
- Control Tower: Issue #1.
- Owner authorization for C source: Issue #1 comment `5811504614`.
- Owner authorization for parallel E source/template: Issue #1 comment `5811700793`.
- Owner transfer of E cloud → local: Issue #1 comment `5813834606`.
- Opus plan: Issue #2 comment `5807784901`.
- CT plan review: Issue #2 comment `5807941253`.
- Current handoff: `docs/control-tower/handoffs/GRL-20260924-C-REVIEW-READY-E-LOCAL-TAKEOVER-V14.md`.
- Required sentinel: `END_OF_GRL_HANDOFF key=GRL-20260924-C-REVIEW-READY-E-LOCAL-TAKEOVER-V14 sections=9`.

## Checkpoint C

Issue #10 / GRL-009 remains independently review-ready.

DRAFT PR #12 exact head:
`67dd220771bf67f65348e622998e005bf30b26bc`.

Implementation handoff:
Issue #10 comment `5813138169`.

No C merge before independent PASS.

## Checkpoint E transfer state

Issue #11 / GRL-010.

Previous cloud implementation claim:
`5812970520` — CANCELLED/REVOKED by owner transfer `5813834606`.

Transfer publication:
Issue #11 comment `5813835204`.

Existing E branch:
`feat/grl-e-execution-template`.

At transfer, that branch head was:
`cd6f6abd13de9af2122d1c61db774b2dc54f7da0`.

It was identical to its base:
- zero E source commits;
- zero changed files;
- no E PR;
- no E implementation handoff.

A local Codex worker may reuse this existing empty branch after fresh-reading current authority and taking a NEW E implementation claim.

Any later publication from the cancelled cloud worker is stale unless explicitly reconciled by CT.

## E source authority

E still uses:
- full Issue #11 packet through its 18-section sentinel;
- CT scope correction `5811742857`;
- E-B1 blocker `5811944203`;
- CT protocol clarification `5812014790`;
- D27 exact-SHA refusal semantics.

Absolute E feature-branch write scope:
`templates/execution-repo/**` ONLY.

No root workflow/schema/Core/docs source changes are allowed on E branch.

## E local proof

Local Codex may run the complete Node campaign:
- Node built-in tests;
- template linter;
- PASS fixture;
- FAIL fixture;
- exact-SHA refusal path;
- schema mirror check;
- `git diff --check`.

No Windows-specific proof is required for E.

Evidence ceiling remains `LOCAL_CHECKED`.

## Activation boundaries

Still forbidden:
- private execution repo creation/use;
- copying/activating template into a live root workflow;
- GitHub Actions dispatch;
- runner registration/start;
- connector-trigger live proof;
- Stage-2 credentials;
- D/G1/service/release work.

## Open gates

D09 / OD-1 private execution repo: OPEN.
D10 / OD-2 GitHub App creation/visibility: OPEN.
D21 Stage-2 credentials: OPEN.
D12 service identity/helper: OPEN.
D15 license/signing: OPEN.

## Next

One local Codex worker may take over E on the existing empty branch `feat/grl-e-execution-template`, post a fresh claim, implement the complete Issue #11 packet + both CT corrections, run local Node proof, publish one DRAFT PR + 9-section handoff, release, and stop.

In parallel, a separate reviewer may continue the exact-head C review.

