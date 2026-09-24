# github-runner-local — C + E parallel; E-B1 resolved (V12)

## 1. Phase

**B_MERGED / C_SOURCE_READY + E_SOURCE_READY_AFTER_PROTOCOL_CLARIFICATION.** E-B1 is resolved durably without changing root Core/schema. No E implementation branch exists yet.

## 2. Authority/read order

Read main `AGENTS.md` → main `docs/CURRENT.md` → this handoff → Issue #1 latest claims → the task issue being claimed.

C authority: Issue #10 / D25.

E authority: Issue #11 / D26 + D27.

For E, required durable comments:
- packet body through 18-section sentinel;
- scope correction `5811742857`;
- blocker E-B1 `5811944203`;
- protocol clarification `5812014790`.

## 3. Stable base

B is merged at `09c87d00e1741083db000ec21220b34841606310`.
The continuity commits after B contain authority/docs only.

C and E remain path-disjoint source tasks.

## 4. Checkpoint C

Issue #10 remains ready, unchanged.

Branch:
`feat/grl-c-device-runner-package`.

Requires Windows contract probe before final C handoff. No live App/login/runner activation.

## 5. Checkpoint E

Issue #11 is ready again.

Branch:
`feat/grl-e-execution-template`.

Write scope ONLY:
`templates/execution-repo/**`.

Cloud Linux implementation/testing is sufficient for E source evidence.

No workflow activation.

## 6. E-B1 resolution

Exact-SHA mismatch is a PRE-EXECUTION refusal.

If requested A != checked-out B:
- execute no profile process;
- emit no canonical `grl.result.v1`;
- fabricate no tested SHA;
- internal outcome BLOCKED / CHECKOUT_SHA_MISMATCH;
- report separate `grl-exec-refusal v1` negative comment;
- post failing commit status on requested A;
- fail final verdict.

Observer semantics:
- valid fully-published refusal => terminal BLOCKED;
- missing/partial refusal publication => REPORTING_INCOMPLETE;
- valid refusal prevents false INTERRUPTED reconciliation.

Required negative tests are enumerated in clarification `5812014790`.

## 7. Parallel safety

Different agents may claim C and E concurrently.
E must not modify C paths; C must not modify template paths.
No agent widens its own scope to resolve cross-task concerns.

## 8. Open gates

OD-1/D09 private execution repo OPEN.
OD-2/D10 GitHub App creation/visibility OPEN.
D21 Stage-2 credential authority OPEN.
Service/signing gates OPEN.
No D/live-E/G1/F/H work.

## 9. Next

One cloud GPT worker may claim E, implement the complete Issue #11 source packet plus both CT corrections, run Node tests/lint/fixtures, publish one DRAFT PR + handoff, release, and stop. Then a different agent performs independent exact-head review.

END_OF_GRL_HANDOFF key=GRL-20260924-C-E-PARALLEL-E-B1-RESOLVED-V12 sections=9
