# CURRENT — github-runner-local

Updated: 2026-09-25. Phase: **D_SOURCE_NEEDS_RD3_RD4_CORRECTION; OFFICE_G1_GATED**.

## Authority

- Canonical branch: `main`; owner-designated successor Control Tower coordinates on Issue #1.
- Current handoff: `docs/control-tower/handoffs/GRL-20260925-D-RD3-RD4-CORRECTION-DISPATCH-V32.md`.
- Required sentinel: `END_OF_GRL_HANDOFF key=GRL-20260925-D-RD3-RD4-CORRECTION-DISPATCH-V32 sections=6`.

## Source status

C and E are independently reviewed and merged. D is **NOT merged**. SAME DRAFT PR #17, branch `feat/grl-d-portable-lifecycle`, reviewed head `d68fc60e29726df25733acb8f00f3ea3ce104ac6`, source base `be6a34872db8c059de060fafbb4188a0e39e1ce1`, remains open/draft/unmerged at this dispatch. Independent Issue #16 review `5834704379`: **`NEEDS_D_CORRECTION`**. R-D-1 closed; R-D-2 main paths closed; R-D-3 unknown numeric ID strands recovery after partial configure, and R-D-4 stale runner version metadata on Resume block merge.

Office Windows read-only witness Issue #16 `5834244169` passed at old exact head: restore, warnings-as-errors build, two full 365/365 solution tests, 11/11 BatchBoundary, pinned runner CLI probe. It is non-live `LOCAL_CHECKED` proof, not `WINDOWS_TESTED` real integration; it must be renewed if PR head changes. The earlier count "10 Windows cases excluded" was an error; 11 cases were present and executed on Windows.

## Active task

Issue #16 CT packet `5834758700` dispatches ONE bounded R-D-3/R-D-4 correction worker on the SAME branch/PR, expected old head `d68fc60e29726df25733acb8f00f3ea3ce104ac6`. Worker claims and releases on Issue #1. Only Issue #16 source/test allowlist; no self-review, new PR, force push or merge. Following correction, a different exact-head independent review and a new read-only office Windows proof are required before CT decides a separate merge gate.

## Live gates

No hosted Actions until owner re-enables. No live login, runner registration/start, private execution-repo activation, or G1 during D source correction. Office `grl-office` is owner approved by OD-7/D29 for later G1 on private `bohanyt/github-runner-local-exec` only after reviewed D merge. Issue #15 G1 remains gated. Conditional post-G1 personal `grl-personal` remains D30 with one active at a time and separately proved switching in Issue #18; safe automatic Drain or server DELETE is unproved.

## Next

Run the bounded SAME-PR worker from Issue #16 `5834758700`; CT handles worker handoff, different independent rereview, and fresh Windows read-only evidence on the changed head. No live work or merge now.
