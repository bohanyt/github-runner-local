# CURRENT — github-runner-local

Updated: 2026-09-25. Phase: **C_REVIEW_READY + E_NEEDS_CORRECTION**.

## Authority

- Canonical branch: `main`.
- Control Tower: Issue #1.
- Owner authorization for C source: Issue #1 comment `5811504614`.
- Owner authorization for E source/template: Issue #1 comment `5811700793`.
- Opus plan: Issue #2 comment `5807784901`.
- CT plan review: Issue #2 comment `5807941253`.
- Current handoff: `docs/control-tower/handoffs/GRL-20260925-C-REVIEW-READY-E-NEEDS-CORRECTION-V17.md`.
- Required sentinel: `END_OF_GRL_HANDOFF key=GRL-20260925-C-REVIEW-READY-E-NEEDS-CORRECTION-V17 sections=10`.

## Checkpoint C

Issue #10 / GRL-009 remains independently review-ready.

DRAFT PR #12:
- branch `feat/grl-c-device-runner-package`
- exact head `67dd220771bf67f65348e622998e005bf30b26bc`
- open / draft / unmerged
- implementation handoff `5813138169`
- worker release `5813145195`

C worker evidence:
- Core 192/192
- Presentation 45/45
- Integration 45/45
- zero skipped
- Windows RunnerContractProbe PASS
- runner v2.337.0 exact pin/hash verified

No independent C review result has been published yet.

Latest connector metadata returned PR #12 mergeable=false; treat that as a fresh-check item, not authority to repair or merge. No C merge is authorized before independent PASS.

## Checkpoint E candidate and review

Issue #11 / GRL-010.

DRAFT PR #13:
- branch `feat/grl-e-execution-template`
- exact reviewed head `b23d37c4f0367d5311502d2cf80e31453a884d1d`
- open / draft / unmerged
- 33 template-only paths
- implementation handoff `5824305934`
- implementation release `5824311312`

Independent review:
- claim `5824425643`
- review result `5824543363`
- release `5824545439`
- disposition `NEEDS_E_CORRECTION`
- blockers: `R-E-1`, `R-E-2`, `R-E-3`

## E correction packet

CT correction packet:
Issue #11 comment `5824981620`.

SAME branch / SAME DRAFT PR #13 only.

Old reviewed head:
`b23d37c4f0367d5311502d2cf80e31453a884d1d`.

Correction write scope is exactly six files:
- `templates/execution-repo/lib/execution.mjs`
- `templates/execution-repo/tests/execution.test.mjs`
- `templates/execution-repo/lib/admission.mjs`
- `templates/execution-repo/tests/admission.test.mjs`
- `templates/execution-repo/tools/lint.mjs`
- `templates/execution-repo/tests/lint.test.mjs`

Everything else is frozen.

## E correction requirements

R-E-1:
- JUnit must not ignore later suites;
- TRX must not silently ignore timeout/aborted/notRunnable or contradictory counters;
- fail closed; add explicit regressions.

R-E-2:
- reconciliation dedupe trusts only exact, strict, bot-authored reconciliation notes;
- human/malformed/extra-field/wrong-identity notes cannot suppress durable reconciliation.

R-E-3:
- linter catches named-step shell `run:`, extra workflow triggers, literal/expression repository overrides, and Stage-2 credential behavior in executable local source;
- add real negative mutation tests.

Full Node/test/lint/schema/PASS/FAIL/E-B1/diff proof is required on corrected exact head.

## Activation boundaries

Still forbidden:
- private execution repo creation/use;
- live template activation;
- GitHub Actions dispatch;
- runner registration/start;
- Stage-2 credentials;
- GitHub App/live login unless separately authorized;
- D/G1/service/release work.

## Open gates

D09 / OD-1 private execution repo: OPEN.
D10 / OD-2 GitHub App creation/visibility: OPEN.
D21 Stage-2 credentials: OPEN.
D12 service identity/helper: OPEN.
D15 license/signing: OPEN.

## Next

Primary next action: one bounded local implementation worker takes the E correction packet `5824981620`, posts a fresh correction claim, fixes exactly R-E-1/R-E-2/R-E-3 in the six authorized files, runs full local proof, pushes SAME branch/PR #13, publishes correction handoff, releases, and stops.

In parallel, a separate independent reviewer may still review exact C PR #12. After E correction, a different independent reviewer performs exact-head E correction rereview.

