# CURRENT — github-runner-local

Updated: 2026-09-27. Phase: **GRL015_CT_ACCEPTED; OFFICE_ID3_HEALTHY; GRL014_SWITCH_DESIGN_READY; PERSONAL_ENROLLMENT_GATED**.

## Authority

- Canonical branch: main; continuing owner-designated Control Tower coordinates on Issue #1.
- Handoff: `docs/control-tower/handoffs/GRL-20260927-GRL015-ACCEPTED-GRL014-DESIGN-V50.md`.
- Sentinel: `END_OF_GRL_HANDOFF key=GRL-20260927-GRL015-ACCEPTED-GRL014-DESIGN-V50 sections=5`.
- GRL-015 CT acceptance: Issue #21 comment `5852242726`.
- Active next task: **Issue #18 / GRL-014 safe admission fence and office/personal switching design**.
- Issue #18 gate update: `5852242889`.

## Completed office phase

Accepted WINDOWS_TESTED evidence:
- OFFICE_OPERATIONAL_PASS + REOPEN_RELAUNCH_PASS: `5845412219`;
- GRL015_REOPEN_REBOOT_PASS: `5852213379`;
- final operator release: `5852213583`.

Durable refs:
- public main `1e74ffdbe9d64dc96bb30007c72af80c625b6946`;
- PR #22 merge `34ccb0c999873418a5077bd3da793a460901a968`;
- private main `2c8da8834e785ba012001b9427bd68380401bec7`.

Current office runner identity: `grl-office` **ID 3**, version **2.337.0**.
Removed ID 2 is historical only.

Runs `36224932168`, `36235000835`, and `36290339991` are completed/success on the reviewed private SHA.

GRL-015 is complete. Do not rerun G1/source review/live relaunch/reboot witnesses without a concrete regression.

## Current office end state

Operator's final WINDOWS_TESTED report: one intended office runner ID 3 online/idle, no non-completed private runs, owned by the verified product. Stable launcher/use note/checkpoints are preserved locally.

Keep ID 3 healthy and active while Issue #18 design work proceeds.

## Issue #18 gate

Issue #18 is **design/evidence only**.

Next worker may research/design the safe admission fence and one-active-at-a-time protocol, but may NOT:
- register `grl-personal`;
- Stop Now/unregister office ID 3;
- call live DELETE;
- change private workflow/variables/labels;
- perform a live switch;
- treat `busy=false` as a drain guarantee.

A later implementation/live packet requires separate explicit authorization.

## Standing safety

ARCHIVE/CHECKPOINT FIRST.
No historical ID 2 as an active target.
No hosted Actions, service/autostart, security/sleep changes, arbitrary/cross-repo work, credential exposure, global auth/env reset or destructive cleanup.
