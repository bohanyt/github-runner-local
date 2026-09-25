# CURRENT — github-runner-local

Updated: 2026-09-25. Phase: **C_REVIEW_READY + E_CORRECTION_REVIEW_READY**.

## Authority

- Canonical branch: `main`.
- Control Tower: Issue #1.
- Owner authorization for C source: Issue #1 comment `5811504614`.
- Owner authorization for E source/template: Issue #1 comment `5811700793`.
- Opus plan: Issue #2 comment `5807784901`.
- CT plan review: Issue #2 comment `5807941253`.
- Current handoff: `docs/control-tower/handoffs/GRL-20260925-C-REVIEW-READY-E-CORRECTION-REVIEW-READY-V18.md`.
- Required sentinel: `END_OF_GRL_HANDOFF key=GRL-20260925-C-REVIEW-READY-E-CORRECTION-REVIEW-READY-V18 sections=10`.

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

Fresh connector metadata currently reports PR #12 mergeable=true. This does not authorize merge; fresh-check again before any later merge action and require independent PASS first.

## Checkpoint E corrected candidate

Issue #11 / GRL-010.

SAME DRAFT PR #13:
- branch `feat/grl-e-execution-template`
- old reviewed head `b23d37c4f0367d5311502d2cf80e31453a884d1d`
- corrected exact head `855bd3342376ca7695a7d19d119d578986d09c2f`
- open / draft / unmerged
- full PR remains exactly 33 template-only paths
- fresh connector metadata currently reports mergeable=true

Prior independent review:
- result `5824543363`
- release `5824545439`
- disposition `NEEDS_E_CORRECTION`
- blockers `R-E-1`, `R-E-2`, `R-E-3`

Correction:
- CT packet `5824981620`
- worker claim `5825042626`
- correction handoff `5825291040`
- worker release `5825294298`
- old→new delta exactly six authorized files

Corrected-head worker-local proof:
- Node v22.16.0
- 106 passed / 0 failed / 0 skipped
- real linter PASS
- schema mirrors byte-for-byte PASS
- PASS fixture 1 process / PASS
- FAIL fixture 1 process / expected FAIL
- E-B1 refusal 0 profile processes / BLOCKED
- targeted R-E-1/R-E-2/R-E-3 regressions PASS
- `git diff --check` PASS

## E correction rereview

Independent rereview packet:
Issue #11 comment `5825299636`.

Exact rereview target:
`855bd3342376ca7695a7d19d119d578986d09c2f`.

The rereviewer must be DIFFERENT from the correction worker and Control Tower, fresh-claim on Issue #1, independently verify all three original findings are closed, publish one exact-head disposition on Issue #11, release, and stop.

Do not merge E before independent PASS.

## Activation boundaries

Still forbidden:
- private execution repo creation/use;
- live template activation;
- GitHub Actions dispatch/rerun;
- runner registration/start;
- Stage-2 credentials;
- GitHub App creation/live login unless separately authorized;
- D/G1/service/UAC/release work.

## Open gates

D09 / OD-1 private execution repo: OPEN.
D10 / OD-2 GitHub App creation/visibility: OPEN.
D21 Stage-2 credentials: OPEN.
D12 service identity/helper: OPEN.
D15 license/signing: OPEN.

## Next

Primary next action: one DIFFERENT independent E correction rereviewer follows Issue #11 packet `5825299636` against exact head `855bd3342376ca7695a7d19d119d578986d09c2f`.

In parallel, a separate independent reviewer may review exact C PR #12 head `67dd220771bf67f65348e622998e005bf30b26bc`.

After either result, Primary Control Tower fresh-checks refs/claims and records the exact disposition. Merge nothing without independent PASS.
