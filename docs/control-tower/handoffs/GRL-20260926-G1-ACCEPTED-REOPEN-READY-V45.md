# github-runner-local — G1 accepted; reopen/restart reliability next (V45)

## 1. Phase and authority

**G1_OFFICE_PASS_CT_ACCEPTED; POST_G1_REOPEN_RESTART_READY.** Office G1 is accepted by CT in Issue #15 comment `5842517893`, based on final operator evidence `5842484351` and released claim `5842487360`.

Active task is Issue #21, **GRL-015 — Post-G1 reopen/restart reliability on existing portable runner**, read IN FULL through:
`END_OF_GRL_REOPEN_PACKET key=GRL-015-PLANNED-REOPEN-REBOOT-20260926 sections=9`.

Issue #15 is no longer the active task. Do not reopen G1 unless a concrete regression invalidates accepted evidence.

## 2. Accepted G1 evidence

CT independently rechecked the durable GitHub state:
- normal positive run `36208004263`: completed/success;
- historical non-mailbox prefilter run `36202109916`: completed/skipped;
- FAULT_INJECTED mismatch run `36212223535`: completed/failure as designed;
- requested R `3ca0449e39fdc28cf5ca15b30967833a95649192` carries failing `grl/js-smoke` status;
- private main is restored at `2c8da8834e785ba012001b9427bd68380401bec7`;
- current private workflow blob is the reviewed baseline `71e0271ee9c1146ebcdc2ac9d982b32c1a773b5d`.

Operator evidence establishes BLOCKED/CHECKOUT_SHA_MISMATCH, profile_executed:false, zero profile execution, refusal reporting, failing verdict, and NORMAL_CONFIRMED restoration. G1 is complete for the reviewed Stage-1 same-repo trusted profile.

PR #19 remains draft/unmerged and preserved as acceptance-tool history. It is not required to ship or merge merely because its exact tool was used for G1.

## 3. Current product gap

Main source deliberately treats reopened roots as recovery-only:
- existing root produces `RECOVERY_REQUIRED`;
- reopened state does not adopt/start the local runner;
- normal `ResumeAsync` requires the same in-memory lifecycle in `Paused`.

Therefore a fresh app process cannot yet reuse the exact registered runner after a planned app close or reboot without falling back to recovery-only handling.

Issue #21 defines the bounded correction: planned Paused recovery only, exact numeric ID/root/repo, no duplicate configuration, and fail-closed treatment of unplanned survivor/uncertain states.

## 4. Sequence

**SEQUENTIAL:**
ONE broad implementation lease on one branch/DRAFT PR -> ONE focused independent review -> CT merge/resume -> SAME office Opus live close/relaunch acceptance -> planned reboot acceptance -> then Issue #18 switching.

No artificial handoffs inside the implementation lease. No full-project re-audit unless the actual delta requires it.

The implementation phase does NOT authorize live Stop Now, closing the current wizard, or rebooting. Preserve the currently proven runner state until source review/merge opens the live witness gate.

## 5. Safety and boundaries

ARCHIVE/CHECKPOINT FIRST.

Preserve runner registration/root/credentials, private execution history, G1 comments/runs/statuses and public branches/PRs. No delete/reset/reinstall to obtain a clean state.

No hosted Actions, service/autostart requirement, UAC/security/sleep-policy changes, personal runner, Stage-2/cross-repo/arbitrary jobs, global auth/env reset or credential copying.

NF-2 remains deferred unless it directly blocks the new planned recovery/resume path. Planned Stop Now remains owner-visible and warned; idle/queue checks are snapshots, not proof of graceful drain.

END_OF_GRL_HANDOFF key=GRL-20260926-G1-ACCEPTED-REOPEN-READY-V45 sections=5
