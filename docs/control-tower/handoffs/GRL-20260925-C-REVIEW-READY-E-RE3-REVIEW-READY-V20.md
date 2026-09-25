# github-runner-local — C review-ready; E R-E-3 rereview-ready (V20)

## 1. Phase

**C_REVIEW_READY + E_RE3_REVIEW_READY.**

C remains review-ready. E completed the second bounded correction and is ready for a different independent exact-head rereview.

## 2. Authority / read order

Fresh-read:
1. main `AGENTS.md`
2. main `docs/CURRENT.md`
3. this handoff through its sentinel
4. Issue #1 latest claims/releases
5. relevant task issue

C: Issue #10.
E: Issue #11.

## 3. C state

DRAFT PR #12:
- branch `feat/grl-c-device-runner-package`
- head `67dd220771bf67f65348e622998e005bf30b26bc`
- open/draft/unmerged
- review packet `5825315282`

No C review result yet.

## 4. E lineage

Original review `5824543363` found R-E-1/R-E-2/R-E-3.

First correction head:
`855bd3342376ca7695a7d19d119d578986d09c2f`.

First correction rereview `5825400160`:
- R-E-1 CLOSED
- R-E-2 CLOSED
- R-E-3 still OPEN

Second correction packet:
`5825431498`.

Second correction handoff:
`5825526198`.

Second correction release:
`5825529400`.

## 5. E exact candidate

SAME DRAFT PR #13.

Exact head:
`e2fb5a3152998990ea12e92be4aef921d1d19387`.

Old→new second-correction delta:
exactly two files:
- `templates/execution-repo/tools/lint.mjs`
- `templates/execution-repo/tests/lint.test.mjs`

Full PR remains 33 template-only paths.

## 6. E evidence

Worker-local exact-head proof:
- Node v24.14.1
- 108 passed / 0 failed / 0 skipped
- real linter PASS
- schema mirror PASS
- PASS fixture PASS
- FAIL fixture expected FAIL
- E-B1 zero-process BLOCKED
- R-E-1/R-E-2 targeted regressions still PASS
- exact multiRepoToken/API bypass now rejected
- crossRepo/multi-repo PAT/generic PAT + other-repo API mutations reject
- baseline legitimate Stage-1 GitHubApi remains PASS
- diff-check clean

## 7. E rereview focus

Packet:
Issue #11 `5825573159`.

A different independent reviewer verifies:
- R-E-3 CLOSED;
- R-E-1/R-E-2 remain CLOSED;
- exact two-file correction delta;
- full template-only scope;
- exact former multiRepoToken/API bypass fails real linter;
- representative cross/multi-repo credential/API bypasses fail;
- legitimate Stage-1 source still passes;
- full source proof and preserved E-B1/no-activation boundaries.

## 8. Review outcomes

Allowed E final dispositions:
- `PASS_E_RE3_CORRECTION_EXACT_HEAD`
- `NEEDS_E_RE3_CORRECTION_AGAIN`
- `STALE_E_RE3_REREVIEW_TARGET`
- `BLOCKED_E_RE3_REREVIEW`

No reviewer may merge.

## 9. Boundaries / parallelism

C review and E rereview may run in parallel.

No activation, hosted Actions, private execution repo, runner activation, Stage-2 implementation, D/G1/service/UAC/release.

## 10. Next

Run independent C review and independent E exact-head rereview.

After either result, Primary CT updates authority. Merge decisions remain separate and require PASS.

END_OF_GRL_HANDOFF key=GRL-20260925-C-REVIEW-READY-E-RE3-REVIEW-READY-V20 sections=10
