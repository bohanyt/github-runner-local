# github-runner-local — C review-ready; E R-E-3 correction ready (V19)

## 1. Phase

**C_REVIEW_READY + E_RE3_CORRECTION_READY.**

C remains independently review-ready. E has one remaining blocker after a successful first correction/rereview cycle.

## 2. Authority / read order

Successor must fresh-read:

1. main `AGENTS.md`
2. main `docs/CURRENT.md`
3. this handoff through its counted sentinel
4. Issue #1 latest claims/releases
5. Issue #10 or #11 depending on task

For E correction also read:
- original review `5824543363`
- first correction packet `5824981620`
- first correction handoff `5825291040`
- correction rereview `5825400160` IN FULL
- rereviewer release `5825402730`
- second correction packet `5825431498` IN FULL

## 3. C state

Issue #10 / GRL-009.

DRAFT PR #12:
- head `67dd220771bf67f65348e622998e005bf30b26bc`
- branch `feat/grl-c-device-runner-package`
- open/draft/unmerged
- review packet `5825315282`

No C review result yet.
No merge before independent PASS.

## 4. E state

Issue #11 / GRL-010.

SAME DRAFT PR #13:
- head `855bd3342376ca7695a7d19d119d578986d09c2f`
- branch `feat/grl-e-execution-template`
- open/draft/unmerged
- full PR = 33 template-only paths

First correction proof:
106 passed / 0 failed / 0 skipped; linter/schema/PASS/FAIL/E-B1 proof green.

## 5. R-E-1 / R-E-2

Independent correction rereview `5825400160` confirms:

- **R-E-1 CLOSED**
- **R-E-2 CLOSED**

Do not reopen or modify their implementation without a new concrete defect.

## 6. Remaining R-E-3

R-E-3 remains OPEN only because the runtime-source linter can still miss ordinary multi-repository credential/API behavior.

Exact reproduced bypass:

```js
const multiRepoToken = process.env.GRL_MULTI_REPO_TOKEN;
await fetch("https://api.github.com/repos/owner/other/contents/file", {
  headers: { Authorization: "Bearer " + multiRepoToken }
});
```

The real linter returns PASS for that mutation.

Current exact source does not implement Stage-2; this is a regression-guard deficiency.

## 7. Bounded second correction

Packet:
Issue #11 `5825431498`.

SAME branch / SAME PR only.

Authorized delta exactly:
- `templates/execution-repo/tools/lint.mjs`
- `templates/execution-repo/tests/lint.test.mjs`

No third file.
No dependency.

Required:
- exact reproduced mutation fails;
- representative cross/multi-repo token/PAT forms fail;
- legitimate Stage-1 GitHubApi remains PASS;
- old R-E-3 negative mutations remain caught;
- complete exact-head Node/linter/schema/PASS/FAIL/E-B1 proof reruns.

## 8. Boundaries

No self-review.
No merge.
No activation.
No Actions dispatch/rerun.
No private execution repo.
No runner activation.
No Stage-2 implementation.
No C source edits.
No D/G1/service/UAC/release work.

## 9. Parallelism

E correction and independent C review may proceed in parallel because their paths/roles are disjoint.

After E correction handoff/release, a DIFFERENT independent exact-head E rereviewer is required.

## 10. Next

Preferred immediate worker:
one bounded local Sol/Codex E R-E-3 correction worker.

It must:
- fresh-check PR #13 still at `855bd3342376ca7695a7d19d119d578986d09c2f`;
- claim `GRL-010-E-RE3-CORRECTION-20260925`;
- follow packet `5825431498` exactly;
- touch only the two authorized files;
- run full proof;
- push SAME branch/PR;
- publish correction handoff;
- release;
- stop.

END_OF_GRL_HANDOFF key=GRL-20260925-C-REVIEW-READY-E-RE3-CORRECTION-READY-V19 sections=10
