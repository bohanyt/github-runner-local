# CURRENT — github-runner-local

Updated: 2026-09-25. Phase: **C_REVIEW_READY + E_RE3_REVIEW_READY**.

## Authority

- Canonical branch: `main`.
- Control Tower: Issue #1.
- Owner authorization for C source: Issue #1 comment `5811504614`.
- Owner authorization for E source/template: Issue #1 comment `5811700793`.
- Opus plan: Issue #2 comment `5807784901`.
- CT plan review: Issue #2 comment `5807941253`.
- Current handoff: `docs/control-tower/handoffs/GRL-20260925-C-REVIEW-READY-E-RE3-REVIEW-READY-V20.md`.
- Required sentinel: `END_OF_GRL_HANDOFF key=GRL-20260925-C-REVIEW-READY-E-RE3-REVIEW-READY-V20 sections=10`.

## Checkpoint C

Issue #10 / GRL-009 remains independently review-ready.

DRAFT PR #12:
- branch `feat/grl-c-device-runner-package`
- exact head `67dd220771bf67f65348e622998e005bf30b26bc`
- open / draft / unmerged
- implementation handoff `5813138169`
- worker release `5813145195`
- independent review packet `5825315282`

C worker evidence:
- Core 192/192
- Presentation 45/45
- Integration 45/45
- zero skipped
- Windows RunnerContractProbe PASS
- runner v2.337.0 exact pin/hash verified

No independent C review result has been published yet.

## Checkpoint E review lineage

Issue #11 / GRL-010.

SAME DRAFT PR #13 / branch:
`feat/grl-e-execution-template`.

Original independent review:
- `5824543363` → `NEEDS_E_CORRECTION`
- R-E-1 / R-E-2 / R-E-3

First correction:
- packet `5824981620`
- corrected head `855bd3342376ca7695a7d19d119d578986d09c2f`
- handoff `5825291040`
- release `5825294298`

First correction rereview:
- `5825400160` → `NEEDS_E_CORRECTION_AGAIN`
- R-E-1 CLOSED
- R-E-2 CLOSED
- R-E-3 remained OPEN

Second correction:
- packet `5825431498`
- old head `855bd3342376ca7695a7d19d119d578986d09c2f`
- new exact head `e2fb5a3152998990ea12e92be4aef921d1d19387`
- handoff `5825526198`
- worker release `5825529400`

## E second correction scope

Second correction changed exactly:
- `templates/execution-repo/tools/lint.mjs`
- `templates/execution-repo/tests/lint.test.mjs`

Full PR remains exactly 33 template-only paths.

Worker-local proof at `e2fb5a3...`:
- Node v24.14.1
- 108 passed / 0 failed / 0 skipped
- real linter PASS
- schema mirrors PASS
- PASS fixture PASS
- FAIL fixture expected FAIL
- E-B1 refusal BLOCKED / zero processes
- old R-E-1/R-E-2 targeted regressions PASS
- exact prior multiRepoToken / GRL_MULTI_REPO_TOKEN + authenticated other-repository API mutation now returns `STAGE2_CREDENTIAL`
- representative crossRepo / multi-repo PAT / generic PAT + other-repository API mutations reject
- baseline legitimate Stage-1 source passes
- `git diff --check` clean

## E independent rereview gate

Durable rereview packet:
Issue #11 `5825573159`.

Exact review target:
`e2fb5a3152998990ea12e92be4aef921d1d19387`.

A DIFFERENT independent reviewer must:
- verify R-E-3 closure;
- confirm R-E-1/R-E-2 remain closed;
- verify exact two-file correction delta;
- independently rerun real linter/mutation proofs and full source campaign where possible;
- publish one exact-head disposition;
- release and stop.

No E merge before independent PASS.

## Activation boundaries

Still forbidden:
- private execution repo creation/use;
- live template activation;
- GitHub Actions dispatch/rerun;
- runner registration/start;
- Stage-2 implementation/credentials;
- GitHub App creation/live login unless separately authorized;
- D/G1/service/UAC/release work.

## Open gates

D09 / OD-1 private execution repo: OPEN.
D10 / OD-2 GitHub App creation/visibility: OPEN.
D21 Stage-2 credentials: OPEN.
D12 service identity/helper: OPEN.
D15 license/signing: OPEN.

## Next

C and E may be independently reviewed in parallel.

- C reviewer takes packet `5825315282` against PR #12 head `67dd220771bf67f65348e622998e005bf30b26bc`.
- DIFFERENT E reviewer takes packet `5825573159` against PR #13 head `e2fb5a3152998990ea12e92be4aef921d1d19387`.

After either result, Primary CT fresh-checks claims/refs and updates continuity. Merge nothing without PASS.
