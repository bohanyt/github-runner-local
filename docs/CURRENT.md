# CURRENT — github-runner-local

Updated: 2026-09-25. Phase: **C_REVIEW_READY + E_PASS_MERGE_READY**.

## Authority

- Canonical branch: `main`.
- Control Tower: Issue #1.
- Owner authorization for C source: Issue #1 comment `5811504614`.
- Owner authorization for E source/template: Issue #1 comment `5811700793`.
- Current handoff: `docs/control-tower/handoffs/GRL-20260925-C-REVIEW-READY-E-PASS-MERGE-READY-V21.md`.
- Required sentinel: `END_OF_GRL_HANDOFF key=GRL-20260925-C-REVIEW-READY-E-PASS-MERGE-READY-V21 sections=8`.

## Checkpoint C

Issue #10 / PR #12 remains independently review-ready.

- branch: `feat/grl-c-device-runner-package`
- exact head: `67dd220771bf67f65348e622998e005bf30b26bc`
- open / draft / unmerged
- review packet: Issue #10 `5825315282`

No C merge before independent PASS.

## Checkpoint E

Issue #11 / PR #13.

Exact passing head:
`e2fb5a3152998990ea12e92be4aef921d1d19387`.

Independent rereview:
- result `5825645126`
- release `5825647808`
- disposition `PASS_E_RE3_CORRECTION_EXACT_HEAD`
- findings = 0
- R-E-1 CLOSED
- R-E-2 CLOSED
- R-E-3 CLOSED

Independent reproduction:
- Node v24.14.1
- 108 passed / 0 failed / 0 skipped
- linter PASS
- schema mirrors PASS
- PASS fixture PASS
- FAIL fixture expected FAIL
- E-B1 refusal BLOCKED / zero profile processes
- targeted R-E-1 2/2
- targeted R-E-2 8/8
- exact former multiRepoToken/API bypass and requested variants reject
- full PR remains 33 template-only paths

PR #13 remains open/draft/unmerged. Fresh metadata reports mergeable=true.

## Merge gate

E is now review-clean and may enter a separate owner/CT merge action.

Before any merge:
- fresh-check exact PR #13 head remains `e2fb5a3...`;
- fresh-check mergeability and claims;
- no hosted Actions;
- no activation as part of merge;
- preserve exact reviewed head with SHA guard.

## Activation boundaries

Still forbidden without separate owner authorization:
- private execution repo creation/use;
- live template activation;
- GitHub Actions dispatch/rerun;
- runner registration/start;
- Stage-2 implementation/credentials;
- GitHub App creation/live login;
- D/G1/service/UAC/release work.

## Next

1. Run independent C review on PR #12 using packet `5825315282`.
2. Separately, owner/CT may authorize and perform exact-head merge of E PR #13.
3. After C PASS, C gets its own separate merge gate.

