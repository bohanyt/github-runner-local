# github-runner-local — Checkpoint B correction ready (V8)

## 1. Phase

**B_NEEDS_CORRECTION / R-B-1_R-B-2_PACKET_READY.** Checkpoint B implementation exists on DRAFT PR #9, but independent review found two blocking defects limited to test/evidence code. Product architecture/source was otherwise assessed as sound. PR #9 is not merge-authorized.

## 2. Authority and read order

Read main `AGENTS.md` → main `docs/CURRENT.md` → this handoff through its sentinel → Issue #1 latest comments → Issue #8 body, implementation handoff `5810381568`, independent review `5811022199`, and correction packet `5811157965`.

Independent reviewer claim `5810793165` was released by `5811024611`. The prior implementation lease was released by `5810405030`.

## 3. Candidate and review result

SAME DRAFT PR #9:
- branch `feat/grl-b-wpf-shell`
- base `31d823604fa21fc755a1cfbf28e9db90ab8df8ec`
- exact reviewed head `bbceeeeef111d1e5dcc62690c263a43f97b93ac6`
- 19 allowed changed paths
- open, draft, unmerged

Independent review Issue #8 comment `5811022199` disposition: `NEEDS_B_CORRECTION`.

Blocking findings:
- R-B-1 — forbidden-API guard false negatives / incomplete recursive coverage.
- R-B-2 — UiSmoke S1 failure classification can produce BLOCKED + exit 0 for real product regressions.

The reviewer performed no supplemental build/test; implementation evidence remains `LOCAL_CHECKED`.

## 4. Correction packet

Issue #8 comment `5811157965` is the exact correction authority.

Only two files may change relative to reviewed head:
1. `tests/Grl.App.Presentation.Tests/PresentationTests.cs`
2. `tests/Grl.App.UiSmoke/Program.cs`

Fix R-B-1 and R-B-2 only. Non-blocking observations N1–N5 are explicitly out of scope. Do not edit product source, Core, project/package files, docs, authority files, workflows, PR metadata or Checkpoint C.

Required proof includes full solution build/tests, deterministic rerun, Core staying 192/192, corrected Presentation test count, UiSmoke classifier/self-test proof, full S1–S5 rerun, exact two-file diff and clean `git diff --check`.

## 5. Evidence and publication

Evidence ceiling remains `LOCAL_CHECKED`. No hosted Actions.

Correction is published to SAME branch/PR with one Issue #8 correction handoff ending:
`END_OF_GRL_B_CORRECTION key=GRL-007-B-RB1-RB2-CORRECTION-20260924 sections=6`.

The correction worker claims/releases on Issue #1 and must not independently rereview its own correction.

## 6. Constraints

No merge. No Checkpoint C. No runner download/registration, GitHub App/OAuth, private execution repo, real device flow, UAC/service/elevated helper, machine security/power/policy changes, release work, hosted workflows, force push, new branch or new PR. PR #3 remains untouched.

## 7. Next

One local bounded worker may execute GRL-007 quickly on the existing Sol Fast session. After publication and release, a different independent reviewer must rereview the NEW exact PR #9 head. PASS rereview is required before any merge decision.

END_OF_GRL_HANDOFF key=GRL-20260924-B-CORRECTION-READY-V8 sections=7
