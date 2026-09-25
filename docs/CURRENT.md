# CURRENT — github-runner-local

Updated: 2026-09-25. Phase: **C_REVIEW_READY + E_REVIEW_READY**.

## Authority

- Canonical branch: `main`.
- Control Tower: Issue #1.
- Owner authorization for C source: Issue #1 comment `5811504614`.
- Owner authorization for E source/template: Issue #1 comment `5811700793`.
- Opus plan: Issue #2 comment `5807784901`.
- CT plan review: Issue #2 comment `5807941253`.
- Current handoff: `docs/control-tower/handoffs/GRL-20260925-C-E-BOTH-REVIEW-READY-V16.md`.
- Required sentinel: `END_OF_GRL_HANDOFF key=GRL-20260925-C-E-BOTH-REVIEW-READY-V16 sections=9`.

## Checkpoint C review candidate

Issue #10 / GRL-009.

DRAFT PR #12:
- branch `feat/grl-c-device-runner-package`
- base `cd6f6abd13de9af2122d1c61db774b2dc54f7da0`
- exact head `67dd220771bf67f65348e622998e005bf30b26bc`
- open / draft / unmerged / mergeable
- 13 changed paths, all within C allowlist
- implementation handoff `5813138169`
- worker release `5813145195`

C worker evidence:
- Core 192/192
- Presentation 45/45
- Integration 45/45
- 0 skipped
- Windows RunnerContractProbe PASS
- official runner v2.337.0 exact SHA matched
- Listener 2.337.0
- all 12 required CLI capabilities present

C requires one independent exact-head review before merge.

## Checkpoint E review candidate

Issue #11 / GRL-010.

DRAFT PR #13:
- branch `feat/grl-e-execution-template`
- base `8c4f5a32e1255a723775bb62ee07c4fe91772b90`
- exact head `b23d37c4f0367d5311502d2cf80e31453a884d1d`
- open / draft / unmerged / mergeable
- 33 changed paths, all under `templates/execution-repo/**`
- implementation handoff `5824305934`
- worker release `5824311312`

E reconciliation provenance:
- stale cloud head `e13f868be0c42f0327c530a03e2e4ac884e9da75`
- original local head `dbba8b3e25d38fc6a03d8981bc6bcbd3a52c940c`
- selected integrated source head `724b580de2d2795ee1a0f35695cfe73896894942`
- published reconciliation merge `b23d37c4...`
- remote stale lineage preserved as first parent
- selected local lineage preserved as second parent
- published tree exactly matches selected integrated source tree
- branch advanced by normal fast-forward, no force push

E final proof:
- Node v24.14.1
- 98 passed / 0 failed / 0 skipped
- linter PASS
- schema mirrors PASS
- PASS fixture PASS
- FAIL fixture PASS
- E-B1 exact-SHA refusal PASS with zero profile processes
- `git diff --check` PASS
- full-SHA first-party action pins source-verified

E requires one independent exact-head review before merge.

## Parallel review rule

C and E reviews are independent and may run concurrently.

Each reviewer:
- claims only its own exact-head review;
- writes only its own review comment + release;
- does not merge;
- does not alter the other candidate.

If either PR head moves, that review target is stale.

## Activation boundaries

Still forbidden:
- GitHub App creation/live login unless separately authorized;
- private execution repo creation/use;
- live template activation;
- Actions dispatch;
- runner registration/start;
- Stage-2 credentials;
- D/G1/service/release work.

## Open gates

D09 / OD-1 private execution repo: OPEN.
D10 / OD-2 GitHub App creation/visibility: OPEN.
D21 Stage-2 credentials: OPEN.
D12 service identity/helper: OPEN.
D15 license/signing: OPEN.

## Next

Run one independent exact-head review for C PR #12 and one independent exact-head review for E PR #13. They may run in parallel. PASS for each is required before its merge decision.

