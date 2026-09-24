# github-runner-local — A1 correction rereview-ready handoff V5

## 1. Phase

A1 was implemented, independently reviewed, and received one blocking finding R-A1-1. That finding has been corrected on the SAME PR. Phase: **A1_CORRECTED / AWAITING_EXACT_HEAD_REREVIEW**.

## 2. Authority

Read main `AGENTS.md` → `docs/CURRENT.md` → this handoff → Issue #1 latest claims → Issue #4 implementation/review/correction comments → Issue #7.

## 3. Candidate lineage

Base: `3e02566f5dcaeea01f5f34284e21731c71b1144b`.

Original reviewed PR #5 head:
`ae2e23a156695f19d9511736a7758b53d4a1406c`.

Independent review:
Issue #4 comment `5809108582` → `NEEDS_A1_CORRECTION`, finding R-A1-1.

Corrected PR #5 head:
`e21599eda1d4c40c391c2d34546e616e59645144`.

Correction handoff:
Issue #4 comment `5809246654`.

Correction claim released by Issue #1 comment `5809250270`.

## 4. Correction

R-A1-1 concerned `WindowsPathPolicy.IsWithinOwnedRoot`: a normalized bare drive root such as `C:\` was trimmed to `C:`, causing all paths on that drive to appear inside the owned root.

Correction rejects bare drive roots as owned-root containment boundaries before trimming separators.

Only two files changed after the reviewed head:
- `src/Grl.Core/WindowsPathPolicy.cs`
- `tests/Grl.Core.Tests/WindowsPathPolicyTests.cs`

Three regression cases were added for C: and D: drive roots.

## 5. Evidence

Worker-local Windows / .NET 10.0.401:
- build PASS, zero warnings/errors;
- test PASS, 192/192, zero skipped.

Evidence: `LOCAL_CHECKED`.

## 6. Rereview

Issue #7 / GRL-004 is the active bounded rereview packet. Reviewer may reuse the prior independent review for unaffected files and should focus on exact delta plus affected full files.

If PR #5 moves from `e21599eda1d4c40c391c2d34546e616e59645144`, stop as stale.

No hosted Actions. No source edits. PASS does not itself merge the PR.

## 7. Next

One independent reviewer performs GRL-004 and posts one exact-head correction rereview result on Issue #4, releases the claim, and stops. CT then decides merge/correction and next checkpoint.

END_OF_GRL_HANDOFF key=GRL-20260924-A1-CORRECTION-REREVIEW-READY-V5 sections=7
