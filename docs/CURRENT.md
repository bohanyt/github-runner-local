# CURRENT — github-runner-local

Updated: 2026-09-26. Phase: **NORMAL_POSITIVE_PASS; FINAL_MISMATCH_READY; G1_INCOMPLETE**.

## Authority

- Canonical branch: main; continuing owner-designated Control Tower coordinates on Issue #1.
- Handoff: `docs/control-tower/handoffs/GRL-20260926-ANCHORLESS-FINAL-G1-V44.md`.
- Sentinel: `END_OF_GRL_HANDOFF key=GRL-20260926-ANCHORLESS-FINAL-G1-V44 sections=5`.
- Active Issue #15 final packet **`5841981163`**, FULL through `END_OF_GRL_FINAL_G1_PACKET key=GRL-OFFICE-G1-ANCHORLESS-FINAL-20260926 sections=5`.

## Working path is real

Normal positive live acceptance passed in Issue #15 `5841913208`: private main `dfb1dfd6b41d2ae951ae555ec751be2527230742`, request `5841882572`, ACK `5841886011`, run `36208004263`, result `5841899241`, successful status/verdict on grl-office ID 2, runner v2.337.0, non-elevated. NF-1 is merged/deployed. This proves GitHub request -> office runner job -> structured result works for the reviewed js-smoke profile.

Historical non-mailbox prefilter run `36202109916` remains accepted because the workflow prefilter blob is unchanged.

## Final missing proof: anchorless mismatch

Independent review `5841741557` closed witness W-1 and found only the runbook's raw pre-plan anchor push unsafe. CT removes that operation instead of another rewrite.

Use exact reviewed tool from PR #19 `f911e7be18d7c574947416609be4c797c360eada`, unmodified:
- P/O = current private main `dfb1dfd6b41d2ae951ae555ec751be2527230742`;
- R = existing ancestor `3ca0449e39fdc28cf5ca15b30967833a95649192`;
- fresh nonce.

R has no commit statuses; R/O share the reviewed workflow and profile blobs. No anchor commit, no PR #19 merge and no new reviewer loop.

After CT claim release, SAME local Opus takes one broad final-G1 claim and runs plan -> overlay -> one FAULT_INJECTED request -> observation -> restore -> NORMAL_CONFIRMED -> final evidence. Do not stop at micro-stages. If mismatch/reporting and restoration pass, publish G1_OFFICE_PASS and release. Otherwise publish the exact blocker without expanding scope automatically.

## Safety and after G1

Preserve runner/wizard/root/private history/settings and credentials. ARCHIVE/CHECKPOINT FIRST. No hosted Actions, arbitrary/cross-repo work, service/UAC/security/sleep changes, global auth/env reset, personal runner or implicit Stop Now/unregister/reboot/reinstall.

After G1: reopen/restart reliability first, then separate Issue #18 one-active-at-a-time switching. NF-2 remains deferred.
