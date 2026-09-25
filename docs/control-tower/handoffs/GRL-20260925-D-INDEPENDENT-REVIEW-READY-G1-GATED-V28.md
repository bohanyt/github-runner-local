# github-runner-local — D independent review ready, office G1 gated (V28)

## 1. Phase

**D_INDEPENDENT_REVIEW_READY; OFFICE_G1_GATED.**

Checkpoint D source is published as a DRAFT PR. It has worker `LOCAL_CHECKED` evidence, not independent approval or live acceptance.

## 2. Authority and exact candidate

Public main at dispatch: `be6a34872db8c059de060fafbb4188a0e39e1ce1`.

Issue #16 / GRL-013 is the D source task. DRAFT PR #17 uses branch `feat/grl-d-portable-lifecycle`, base at that main, exact candidate head `b14ff5fa108a4a1891efbc03f700bbffaaa3cfcf`. At dispatch it is open, draft and unmerged.

Worker handoff: Issue #16 comment `5827081178`. Worker release: Issue #1 comment `5827088164`.

## 3. Worker evidence

Reported: restore PASS; warnings-as-errors build 0 warnings/errors; full solution tests twice 352/352 each, Core 192, Presentation 53, Integration 107, zero failed/skipped; existing Windows runner contract probe and production-adapter read-only `config.cmd --help` witness PASS; `git diff --check` PASS.

PR has 19 changed paths, all within the Issue #16 source/test/developer-build allowlist on CT filename inspection. This does not substitute for full independent diff review.

No live Device Flow, runner registration/start, execution template activation, self-review or merge was reported.

## 4. Mandatory review focus

Issue #16 independent review packet `5827801807` requires review of the whole PR at exact head, including admin/token handling, Windows batch boundary, lifecycle, presentation, tests and evidence limitations.

The worker flagged a drain race. Current `DrainAsync` stops after an absent or idle remote snapshot, and `StopAsync` kills the owned process tree. Reviewer must decide whether that can kill a newly assigned job or misread stale status, and whether the contract can be met without an atomic admission fence.

## 5. Owner-approved live topology

OD-1/D09: private execution repo `bohanyt/github-runner-local-exec`.

OD-2/D10: owner-only Device Flow App for acceptance, with runtime Client ID on Issue #14; App installation settings owner-attested.

OD-7/D29: current office Windows laptop approved for first portable, non-admin, trusted-code-only G1 runner `grl-office`.

D30: after G1 PASS only, personal runner `grl-personal` may be enrolled separately, with exactly ONE active matching `grl-exec` runner at a time. Never copy runner credentials.

## 6. Gated live task

Issue #15 / GRL-012 remains gated. A source PR, local test report, or reviewer PASS cannot count as a real G1 witness. D source must pass independent exact-head review, any corrections, and a separate CT/owner merge gate before returning to the office laptop's live Device Flow and self-hosted fixture campaign.

Evidence may advance to `WINDOWS_TESTED` only on actual live integration acceptance.

## 7. Boundaries

No GitHub-hosted Actions, Stage-2/cross-repo checkout or credential, public App distribution, service/UAC/elevated helper, arbitrary shell inbox, external project workload, personal laptop enrollment before G1 PASS, signing/release.

Reviewer has review-only writes: Issue #1 claim/release and one Issue #16 result. No source edits, branch/PR mutation, merge, live login, runner registration/start or private execution repo activation.

## 8. Next

One different independent reviewer follows Issue #16 packet `5827801807` at exact PR #17 head `b14ff5fa108a4a1891efbc03f700bbffaaa3cfcf`, publishes findings/disposition and releases. CT then dispatches bounded correction if needed; only an independent PASS can open the separate merge gate. Office G1 remains gated.

END_OF_GRL_HANDOFF key=GRL-20260925-D-INDEPENDENT-REVIEW-READY-G1-GATED-V28 sections=8
