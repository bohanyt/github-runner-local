# github-runner-local — Checkpoint B corrected, rereview ready (V9)

## 1. Phase

**B_CORRECTED / AWAITING_INDEPENDENT_EXACT_HEAD_REREVIEW.** The two blocking findings from the first B review were corrected on the SAME DRAFT PR #9. No merge authorization exists.

## 2. Authority and lineage

Read main `AGENTS.md` → main `docs/CURRENT.md` → this handoff → Issue #1 latest comments → Issue #8 implementation, review, correction packet and correction handoff.

- Implementation handoff: `5810381568`
- Independent review: `5811022199` → `NEEDS_B_CORRECTION`
- Correction packet: `5811157965`
- Correction handoff: `5811298851`
- Correction release: Issue #1 `5811303475`

## 3. Candidate refs

SAME DRAFT PR #9:
- branch `feat/grl-b-wpf-shell`
- base `31d823604fa21fc755a1cfbf28e9db90ab8df8ec`
- old reviewed head `bbceeeeef111d1e5dcc62690c263a43f97b93ac6`
- corrected head `7b7472f910ae54e2de813f90d7d5fa3a60d5f218`
- open, draft, unmerged

Exactly two files changed after the reviewed head:
1. `tests/Grl.App.Presentation.Tests/PresentationTests.cs`
2. `tests/Grl.App.UiSmoke/Program.cs`

## 4. Corrections

R-B-1: strengthened the forbidden-API guard to catch native-import metadata, `Environment.GetFolderPath` / nested `SpecialFolder`, and recursive Grl.App source paths while excluding generated output; added regression self-tests.

R-B-2: fixed S1 outcome classification so real product regressions become FAIL rather than BLOCKED/exit-0; added deterministic classifier self-tests.

Non-blocking N1–N5 were intentionally not fixed. Product source, Core, project files, docs, workflows and Checkpoint C were untouched.

## 5. Evidence

Local Windows evidence at corrected head:
- build: PASS, 0 warnings/errors
- Core tests: 192/192, 0 skipped
- Presentation tests: 45/45, 0 skipped
- UiSmoke classifier self-test: 7/7
- full fake-mode UIA smoke: S1 PASS, S2 PASS, S3 PASS, S4 PASS, S5 PASS
- correction diff scope: exactly the two allowed files
- diff-check clean

Evidence remains `LOCAL_CHECKED`.

## 6. Rereview contract

A different independent reviewer should reuse the prior review for unaffected files and inspect only:
- exact diff `bbceeee...7b7472f`
- full corrected two files
- closure of R-B-1 and R-B-2
- any new blocker directly introduced by the correction

If PR #9 moves from the corrected head before verdict publication, stop stale. PASS does not merge the PR.

## 7. Next

Publish one independent exact-head rereview result on Issue #8, release the reviewer claim on Issue #1, and stop. PASS is required before CT/owner merge decision. Checkpoint C remains unauthorized.

END_OF_GRL_HANDOFF key=GRL-20260924-B-CORRECTED-REREVIEW-READY-V9 sections=7
