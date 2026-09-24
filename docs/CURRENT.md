# CURRENT — github-runner-local

Updated: 2026-09-24. Phase: **B_MERGED / C_SOURCE_READY + E_SOURCE_READY_AFTER_PROTOCOL_CLARIFICATION**.

## Authority

- Canonical branch: `main`.
- Control Tower: Issue #1.
- Owner authorization for B merge + C source: Issue #1 comment `5811504614`.
- Owner authorization for parallel E source/template: Issue #1 comment `5811700793`.
- Opus plan: Issue #2 comment `5807784901`.
- CT plan review: Issue #2 comment `5807941253`.
- Current handoff: `docs/control-tower/handoffs/GRL-20260924-C-E-PARALLEL-E-B1-RESOLVED-V12.md`.
- Required sentinel: `END_OF_GRL_HANDOFF key=GRL-20260924-C-E-PARALLEL-E-B1-RESOLVED-V12 sections=9`.

## Completed base

Checkpoint B remains merged and complete at merge commit `09c87d00e1741083db000ec21220b34841606310`.

## Parallel source tasks

### C — Issue #10 / GRL-009

Still ready for a later local Windows worker on branch:
`feat/grl-c-device-runner-package`.

C authority and constraints are unchanged.

### E — Issue #11 / GRL-010

Cloud-friendly source/template task.

Branch:
`feat/grl-e-execution-template`

Write scope:
`templates/execution-repo/**` ONLY.

Durable packet:
Issue #11 through
`END_OF_GRL_E_PACKET key=GRL-010-E-EXECUTION-TEMPLATE-SOURCE-20260924 sections=18`.

Parallel-scope correction:
Issue #11 comment `5811742857`.

Preimplementation blocker:
Issue #11 comment `5811944203` (E-B1).

CT protocol resolution:
Issue #11 comment `5812014790`.

D27 makes the resolution durable.

## E-B1 resolved semantics

For requested SHA A but checked-out HEAD B:

- zero profile processes execute;
- no canonical `grl.result.v1` / result artifact is emitted;
- no `tested_sha` is fabricated;
- source emits internal BLOCKED / `CHECKOUT_SHA_MISMATCH`;
- report publishes bounded `grl-exec-refusal v1` negative evidence and a failing `grl/<profile>` commit status on requested SHA A;
- verdict fails;
- complete refusal publication is terminal BLOCKED;
- missing/partial refusal publication is REPORTING_INCOMPLETE.

Root Core/schema remain unchanged.

## E activation remains forbidden

No private execution repo, root workflow activation, Actions dispatch, runner registration/start, connector-trigger live proof, or Stage-2 credential work.

## Open gates

Still OPEN:
- D09 / OD-1 private execution repo;
- D10 / OD-2 GitHub App creation/visibility;
- D21 Stage-2 credentials;
- D12 service identity/helper;
- D15 license/signing.

D, live E activation, G1, service mode and release remain unauthorized.

## Constraints

No GitHub-hosted Actions. No root workflow changes. No repository settings changes. No live runner/App/login activation. No security/power/policy changes.

## Next

A cloud GPT worker may now claim Issue #11 and implement E source completely, applying both CT corrections `5811742857` and `5812014790`, publish one DRAFT PR/handoff, release, and stop. Independent exact-head review follows.
