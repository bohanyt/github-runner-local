# CURRENT — github-runner-local

Updated: 2026-09-25. Phase: **C_IMPLEMENTED / AWAITING_INDEPENDENT_EXACT_HEAD_REVIEW + E_RACE_RECONCILIATION_READY**.

## Authority

- Canonical branch: `main`.
- Control Tower: Issue #1.
- Owner authorization for C source: Issue #1 comment `5811504614`.
- Owner authorization for E source/template: Issue #1 comment `5811700793`.
- Owner cloud→local transfer: Issue #1 comment `5813834606`.
- Opus plan: Issue #2 comment `5807784901`.
- CT plan review: Issue #2 comment `5807941253`.
- Current handoff: `docs/control-tower/handoffs/GRL-20260925-C-REVIEW-READY-E-RACE-RECONCILIATION-V15.md`.
- Required sentinel: `END_OF_GRL_HANDOFF key=GRL-20260925-C-REVIEW-READY-E-RACE-RECONCILIATION-V15 sections=9`.

## Checkpoint C

Issue #10 / GRL-009 remains independently review-ready.

DRAFT PR #12 exact head:
`67dd220771bf67f65348e622998e005bf30b26bc`.

No C merge before independent PASS.

## Checkpoint E race

Issue #11 / GRL-010.

Cancelled cloud claim:
`5812970520`.

Owner transfer:
`5813834606`.

Local takeover claim:
`5813930109`, released blocked by `5814361882`.

Blocked handoff:
Issue #11 `5814358849`.

Unexpected remote E head:
`e13f868be0c42f0327c530a03e2e4ac884e9da75`.

That stale cloud commit:
- appeared after claim revocation;
- changes 31 paths, all under `templates/execution-repo/**`;
- has no PR or implementation handoff;
- is not automatically accepted or discarded.

Unpublished local candidate:
`dbba8b3e25d38fc6a03d8981bc6bcbd3a52c940c`.

Local reported proof:
- Node v24.14.1;
- 87 passed / 0 skipped;
- linter PASS;
- schema mirrors PASS;
- PASS/FAIL fixtures PASS;
- E-B1 refusal proof PASS with zero profile processes;
- 33 template paths.

## E reconciliation authority

CT packet:
Issue #11 `5823097901`.

Decision D28 requires evidence-driven no-force reconciliation.

One local Codex reconciliation worker must:
- preserve both commits;
- run the full proof campaign independently on BOTH exact candidates;
- compare complete source against Issue #11 + `5811742857` + `5812014790`;
- select or integrate the strongest compliant tree;
- never force-push.

If local/integrated tree is selected, publication uses a reconciliation merge:
- first parent = current remote E head `e13f868...`;
- second parent = selected local commit;
- tree = exact selected local commit tree;
- push is fast-forward on the canonical E branch.

Absolute E source scope remains:
`templates/execution-repo/**` ONLY.

## E publication gate

No E PR/handoff exists yet.

Final E candidate must have:
- full Node tests;
- linter;
- schema mirror;
- PASS/FAIL fixtures;
- E-B1 refusal proof;
- `git diff --check`;
- exact template-only path proof.

After publication, a different independent reviewer reviews E.

## Activation boundaries

No private execution repo.
No root workflow activation.
No Actions dispatch.
No runner activation.
No Stage-2 credentials.
No D/G1/service/release work.

## Open gates

D09 / OD-1 private execution repo: OPEN.
D10 / OD-2 GitHub App creation/visibility: OPEN.
D21 Stage-2 credentials: OPEN.
D12 service identity/helper: OPEN.
D15 license/signing: OPEN.

## Next

One local Codex worker claims the E reconciliation task, compares `e13f868...` and `dbba8b3...`, publishes the selected reconciled E candidate without force, creates one DRAFT PR + 9-section handoff, releases, and stops.

C review may proceed independently.

