# CURRENT — github-runner-local

Updated: 2026-09-26. Phase: **G1_OFFICE_PASS_CT_ACCEPTED; GRL015_PR22_JUNCTION_CORRECTION_READY**.

## Authority

- Canonical branch: main; continuing owner-designated Control Tower coordinates on Issue #1.
- Handoff: `docs/control-tower/handoffs/GRL-20260926-PR22-JUNCTION-CORRECTION-V47.md`.
- Sentinel: `END_OF_GRL_HANDOFF key=GRL-20260926-PR22-JUNCTION-CORRECTION-V47 sections=5`.
- Active task: Issue #21 / GRL-015.
- Active correction packet: Issue #21 comment **`5842873213`**, FULL through `END_OF_GRL015_CORRECTION_PACKET key=GRL-PR22-JUNCTION-R1-20260926 sections=6`.

## Review result

Independent review `5842855807` at PR #22 head
`7d4ff0f07d4b93562932469b48d1310069f8dc91` found ONE
SAFETY_BLOCKER: the reopened listener path accepts a redirected `bin` junction.

Reviewer independently passed Integration 170/170 and Presentation 64/64, found
0 additional REOPEN_PATH_BLOCKER and 0 new DEFERRED issues, and released claim
`5842857983`.

All other reviewed planned-reopen behavior remains accepted for the correction
cycle.

## Immediate next

SAME local Opus takes one narrow correction claim and continues SAME branch /
SAME DRAFT PR #22. Primary source scope is
`src/Grl.Integration/PortableRunnerProcess.cs` plus targeted Integration
regression(s).

Reject every reparse/junction component on
`root\bin\Runner.Listener.exe` before listener execution and preserve the
immediate ordinary root/run.cmd check before start.

No lifecycle/UX redesign, no G1 rerun, no live Stop Now/close/reboot.

After correction handoff/release: ONE focused rereview -> CT merge/resume -> SAME
Opus live close/relaunch -> planned reboot -> Issue #18.

## Safety

ARCHIVE/CHECKPOINT FIRST. Preserve runner/root/credentials/history, old PR head
and accepted G1 evidence. No hosted Actions, service/autostart, security/sleep
changes, personal runner, Stage-2/cross-repo/arbitrary jobs or global auth/env reset.

NF-2 remains deferred.
