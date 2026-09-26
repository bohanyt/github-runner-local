# CURRENT — github-runner-local

Updated: 2026-09-26. Phase: **G1_OFFICE_PASS_CT_ACCEPTED; POST_G1_REOPEN_RESTART_READY**.

## Authority

- Canonical branch: main; continuing owner-designated Control Tower coordinates on Issue #1.
- Handoff: `docs/control-tower/handoffs/GRL-20260926-G1-ACCEPTED-REOPEN-READY-V45.md`.
- Sentinel: `END_OF_GRL_HANDOFF key=GRL-20260926-G1-ACCEPTED-REOPEN-READY-V45 sections=5`.
- Active task: **Issue #21 — GRL-015 post-G1 reopen/restart reliability**, FULL through `END_OF_GRL_REOPEN_PACKET key=GRL-015-PLANNED-REOPEN-REBOOT-20260926 sections=9`.

## G1 complete

CT accepted G1 in Issue #15 comment `5842517893` after final operator report `5842484351` / release `5842487360`.

Accepted live proof:
- normal positive run `36208004263` success;
- non-mailbox prefilter run `36202109916` skipped;
- FAULT_INJECTED checkout-mismatch run `36212223535` failed as designed with refusal evidence;
- private main restored to `2c8da8834e785ba012001b9427bd68380401bec7`;
- reviewed normal workflow blob `71e0271ee9c1146ebcdc2ac9d982b32c1a773b5d` restored.

Do not reopen G1 for generic cleanup. PR #19 stays preserved/draft as acceptance-tool history.

## Next product gap

Current source detects an existing runner root but treats reopen as recovery-only; a fresh app process cannot resume the persisted exact runner registration. Issue #21 addresses only planned recovery from an explicit Paused/no-survivor state.

Target user flow:
explicit warned Stop Now while no known job -> close app -> relaunch -> sign in if required -> Resume existing runner -> SAME runner ID online -> harmless job PASS; then repeat across a planned reboot.

Unplanned survivor/uncertain state remains fail-closed. No duplicate registration or config.cmd on reopen.

## Sequence

**SEQUENTIAL:** broad implementation/DRAFT PR -> one focused independent review -> CT merge/resume -> same local Opus close/relaunch live proof -> planned reboot proof -> Issue #18 office/personal switching.

Implementation is source-only until the live gate. Do not Stop Now/close/reboot the proven office runner merely to develop the code.

## Standing safety

ARCHIVE/CHECKPOINT FIRST. Preserve registration/root/credentials/history. No hosted Actions, service/autostart, security/sleep changes, personal runner, Stage-2/cross-repo/arbitrary execution or global auth/env reset.

NF-2 stays deferred unless it directly blocks Issue #21.
