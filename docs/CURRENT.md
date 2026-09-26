# CURRENT — github-runner-local

Updated: 2026-09-26. Phase: **G1_OFFICE_PASS_CT_ACCEPTED; GRL015_PR22_INDEPENDENT_REVIEW_READY**.

## Authority

- Canonical branch: main; continuing owner-designated Control Tower coordinates on Issue #1.
- Handoff: `docs/control-tower/handoffs/GRL-20260926-PR22-REOPEN-REVIEW-V46.md`.
- Sentinel: `END_OF_GRL_HANDOFF key=GRL-20260926-PR22-REOPEN-REVIEW-V46 sections=5`.
- Active task: Issue #21 / GRL-015.
- Active review packet: Issue #21 comment **`5842772958`**, FULL through `END_OF_GRL015_REVIEW_PACKET key=GRL-PR22-7D4FF0F-REOPEN-REVIEW-20260926 sections=6`.

## Candidate

DRAFT PR #22:
- `feat/grl-015-planned-reopen-resume`
- exact head `7d4ff0f07d4b93562932469b48d1310069f8dc91`
- base `3ae26872430c0aded342d15cdbac3a315ecb2f8b`
- 12 changed paths, one commit.

Implementation handoff `5842736701`; implementation claim released by `5842738309`.

Worker reports build clean, Integration 170/170, Presentation 64/64, Core 192/192 and six mutation checks caught. These are LOCAL_CHECKED worker evidence until independent review.

## Review gate

ONE separate reviewer now checks the bounded planned-pause recovery/resume delta. No subagents.

Review must verify exact-ID/offline/non-busy recovery eligibility, run.cmd-only existing-root start with no configure/registration token, listener-only version verification, concurrency/duplicate-start prevention, truthful failure states, no adoption of unowned online process, and correct UI eligibility.

No live runner mutation, Stop Now, app close/relaunch or reboot at this gate.

## Sequence

**SEQUENTIAL:** independent PASS -> CT merge/resume -> SAME local Opus close/relaunch acceptance -> planned reboot acceptance -> Issue #18.

G1 remains CT-accepted and closed. Do not rerun G1 or PR #19.

## Safety

ARCHIVE/CHECKPOINT FIRST. Preserve office runner/root/credentials/private history and G1 evidence.

No hosted Actions, service/autostart, security/sleep changes, personal runner, Stage-2/cross-repo/arbitrary jobs or global auth/env reset.

NF-2 stays deferred unless directly implicated by a concrete finding.
