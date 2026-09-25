# CURRENT — github-runner-local

Updated: 2026-09-25. Phase: **C_CORRECTION_REVIEW_READY + E_PASS_MERGE_READY**.

## Authority

- Canonical branch: `main`.
- Control Tower: Issue #1.
- Current handoff: `docs/control-tower/handoffs/GRL-20260925-C-CORRECTION-REVIEW-READY-E-PASS-V22.md`.
- Required sentinel: `END_OF_GRL_HANDOFF key=GRL-20260925-C-CORRECTION-REVIEW-READY-E-PASS-V22 sections=8`.

## Checkpoint C

Issue #10 / SAME DRAFT PR #12.

Original reviewed head:
`67dd220771bf67f65348e622998e005bf30b26bc`.

Independent review:
`5825901999` → `NEEDS_C_CORRECTION`.

Findings:
- R-C-1 OAuth JSON negotiation
- R-C-2 RunnerConfiguration token display leak
- R-C-3 superscript Win32 device aliases

Correction:
- claim `5825976706`
- handoff `5826033449`
- release `5826036772`
- exact corrected head `c062957407acf8f7d70c46767345e85c737f39f4`
- exact correction delta = six authorized source/test files
- full PR remains 13 original allowlisted paths

Correction proof:
- build 0 warnings / 0 errors
- Core 192/192
- Presentation 45/45
- Integration 68/68
- 0 failed / 0 skipped
- targeted correction campaign 23/23
- Windows read-only runner probe PASS
- runner v2.337.0 exact SHA match
- 12 required CLI capabilities present
- no registration/start/token/service

Independent correction rereview packet:
Issue #10 `5826108522`.

No C merge before independent PASS.

## Checkpoint E

Issue #11 / DRAFT PR #13.

Exact passing head:
`e2fb5a3152998990ea12e92be4aef921d1d19387`.

Independent exact-head rereview:
`5825645126` → `PASS_E_RE3_CORRECTION_EXACT_HEAD`, findings=0.

R-E-1 / R-E-2 / R-E-3 are all CLOSED.

E remains open/draft/unmerged and merge-ready pending a separate exact-head owner/CT merge action.

## Merge boundaries

No merge under reviewer roles.

Before any merge:
- fresh-check exact reviewed head;
- fresh-check mergeability and claims;
- use SHA guard;
- no bypass/force;
- no activation or hosted Actions as part of merge.

## Activation boundaries

Still forbidden without separate owner authorization:
- private execution repo
- live template activation
- hosted Actions dispatch/rerun
- runner registration/start
- Stage-2 credentials/implementation
- GitHub App creation/live login
- D/G1/service/UAC/release

## Next

1. One DIFFERENT independent reviewer rereviews C exact corrected head using packet `5826108522`.
2. E may proceed separately to an exact-head merge action when owner/CT authorizes it.
3. After C PASS, C gets its own separate merge gate.
