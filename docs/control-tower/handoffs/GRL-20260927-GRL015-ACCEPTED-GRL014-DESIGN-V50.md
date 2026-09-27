# github-runner-local — GRL-015 accepted; safe switching design next (V50)

## 1. Phase and authority

**GRL015_CT_ACCEPTED; OFFICE_ID3_HEALTHY; GRL014_SWITCH_DESIGN_READY; PERSONAL_ENROLLMENT_GATED.**

Issue #21 / GRL-015 is CT-accepted in comment `5852242726`.

The next active project topic is Issue #18 / GRL-014. CT gate update is comment
`5852242889`.

Issue #18 remains **DESIGN / EVIDENCE ONLY**. It does not authorize personal
runner enrollment or a live switch.

## 2. Accepted office state

Durable refs:
- public main `1e74ffdbe9d64dc96bb30007c72af80c625b6946`;
- PR #22 merge `34ccb0c999873418a5077bd3da793a460901a968`;
- private main `2c8da8834e785ba012001b9427bd68380401bec7`.

Accepted WINDOWS_TESTED operator evidence:
- OFFICE_OPERATIONAL_PASS / REOPEN_RELAUNCH_PASS: Issue #21 `5845412219`;
- GRL015_REOPEN_REBOOT_PASS: Issue #21 `5852213379`;
- final lease release: Issue #1 `5852213583`.

Verified workflow runs:
- `36224932168` completed/success;
- `36235000835` completed/success;
- `36290339991` completed/success;
all at private SHA `2c8da8834e785ba012001b9427bd68380401bec7`.

Current authoritative office identity is `grl-office` numeric **ID 3**, runner
version **2.337.0**. Historical ID 2 was removed and must not be used as a current
target.

The operator's final reported state is ID 3 online/idle, one intended office
runner, no non-completed private runs, owned by the verified product. CT accepts
that as WINDOWS_TESTED operator evidence and does not claim a separate live-machine
observation.

## 3. What is complete

Do not repeat without a concrete regression:
- historical G1;
- PR #22 source review/correction history;
- normal replacement-ID-3 operation witness;
- planned close/relaunch same-ID witness;
- planned Windows reboot same-ID witness.

Stable local launcher, shortcut, Indonesian use note and non-secret checkpoints
are preserved on the office laptop per the accepted operator report.

GRL-015 is complete.

## 4. Issue #18 gate

Issue #18 / GRL-014 is now the next active design/evidence gate.

Goal: define and prove the minimum safe office/personal one-active-at-a-time
switching protocol.

The next bounded worker should:
- preserve office ID 3 online/healthy;
- study the exact admission-fence problem and existing Issue #18 source notes;
- identify a supported mechanism that prevents new assignments before any
  stop/unregister decision;
- bind the protocol to exact repo + numeric runner identity + process;
- cover idle→assigned, overlapping/mid-request, remote uncertainty and recovery;
- distinguish request/workflow admission from runner busy snapshots;
- state whether the viable protocol preserves two registrations or requires
  remove/re-register, and surface any owner decision this changes;
- produce one concrete protocol/evidence plan before implementation.

No live DELETE, Stop Now, unregister, personal enrollment, workflow/variable/label
change or switching witness is authorized by this handoff.

## 5. Standing safety

ARCHIVE/CHECKPOINT FIRST.

Keep office runner ID 3 in its current healthy state.
Do not register `grl-personal` yet.
Do not use historical ID 2.
No hosted Actions, service/autostart, UAC/security/sleep changes, arbitrary or
cross-repo workloads, credential copying/exposure, global auth/env reset, or
destructive cleanup.

Any later source implementation or live switching packet requires a new explicit
authority update after the design/evidence gate.

END_OF_GRL_HANDOFF key=GRL-20260927-GRL015-ACCEPTED-GRL014-DESIGN-V50 sections=5
