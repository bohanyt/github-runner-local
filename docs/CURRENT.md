# CURRENT — github-runner-local

Updated: 2026-09-25. Phase: **C_REVIEW_READY + E_RE3_CORRECTION_READY**.

## Authority

- Canonical branch: `main`.
- Control Tower: Issue #1.
- Owner authorization for C source: Issue #1 comment `5811504614`.
- Owner authorization for E source/template: Issue #1 comment `5811700793`.
- Opus plan: Issue #2 comment `5807784901`.
- CT plan review: Issue #2 comment `5807941253`.
- Current handoff: `docs/control-tower/handoffs/GRL-20260925-C-REVIEW-READY-E-RE3-CORRECTION-READY-V19.md`.
- Required sentinel: `END_OF_GRL_HANDOFF key=GRL-20260925-C-REVIEW-READY-E-RE3-CORRECTION-READY-V19 sections=10`.

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

No independent C review result is published yet.

## Checkpoint E current candidate

Issue #11 / GRL-010.

SAME DRAFT PR #13:
- branch `feat/grl-e-execution-template`
- current exact head `855bd3342376ca7695a7d19d119d578986d09c2f`
- open / draft / unmerged
- full PR remains 33 template-only paths

Original independent review:
- result `5824543363`
- disposition `NEEDS_E_CORRECTION`
- findings `R-E-1`, `R-E-2`, `R-E-3`

First correction:
- packet `5824981620`
- corrected head `855bd3342376ca7695a7d19d119d578986d09c2f`
- handoff `5825291040`
- worker release `5825294298`
- 106 passed / 0 failed / 0 skipped

Independent correction rereview:
- result `5825400160`
- release `5825402730`
- disposition `NEEDS_E_CORRECTION_AGAIN`
- `R-E-1` CLOSED
- `R-E-2` CLOSED
- `R-E-3` remains OPEN

## Remaining R-E-3 defect

The real linter still allows executable multi-repository credential/API behavior such as:

```js
const multiRepoToken = process.env.GRL_MULTI_REPO_TOKEN;
await fetch("https://api.github.com/repos/owner/other/contents/file", {
  headers: { Authorization: "Bearer " + multiRepoToken }
});
```

while returning lint PASS.

The exact current source does not contain Stage-2 behavior; the blocker is the mandatory regression guard.

## Second E correction packet

Durable packet:
Issue #11 comment `5825431498`.

SAME branch / SAME PR #13.

Current exact head before correction:
`855bd3342376ca7695a7d19d119d578986d09c2f`.

Absolute write scope is exactly two files:
- `templates/execution-repo/tools/lint.mjs`
- `templates/execution-repo/tests/lint.test.mjs`

Everything else is frozen.

Required outcome:
- exact rereviewer multiRepoToken / `GRL_MULTI_REPO_TOKEN` + authenticated other-repository API mutation fails with `STAGE2_CREDENTIAL`;
- representative cross-repo/PAT credential forms fail;
- legitimate current-repository Stage-1 GitHubApi remains lint PASS;
- all prior R-E-3 negative mutations remain failing;
- full Node/linter/schema/PASS/FAIL/E-B1 proof stays green.

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

Primary E action: one bounded implementation worker claims and applies Issue #11 packet `5825431498` on SAME branch/PR #13, touching exactly the two authorized linter files, runs full proof, publishes correction handoff, releases, and stops.

In parallel, a separate independent reviewer may still take C packet `5825315282`.

After the E correction, a DIFFERENT independent reviewer rereviews the exact new E head. Merge nothing without PASS.

