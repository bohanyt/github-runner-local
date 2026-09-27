# github-runner-local — PR #24 F1–F4 correction ready (V53)

## 1. Phase and authority

**GRL014_PR24_F1_F4_CORRECTION_READY; LIVE_ACTIVATION_GATED; GRL015_ACCEPTED.**

Active issue: #18 / GRL-014.
Active correction packet: Issue #18 comment `5854201198`, read IN FULL through:
`END_OF_GRL014_CORRECTION_PACKET key=GRL014-PR24-F1-F4-20260927 sections=6`.

Integrated review result: Issue #18 `5853961376`.
Reviewer claim `5853704407` released by `5853963616`.

PR #24 remains the SAME implementation PR at old head
`d0188b845d5cdc01542c4fc12cd63fce37ab3a2f` until the correction is pushed.

## 2. Findings accepted

Four narrow blockers are accepted:
- F1: non-canonical/trailing-separator runner path can hide an orphaned same-runner Worker;
- F2: gate inventory misses equivalent unconditional condition syntax;
- F3: stale `gate.lock` can make warned emergency Stop Now unavailable;
- F4: descendant and orphan-image Worker rules are not independently tested.

Independent reviewer reproduced build/Integration/gate/template/lint/diff checks green at old head; those green results do not override the blockers.

Accepted conclusions for unchanged source are retained. A1/A5 remain deferred live proof.

## 3. Correction worker

The SAME Sol implementation worker may take one fresh broad correction claim and continue:
- SAME branch `feat/grl014-job-gate`;
- SAME DRAFT PR #24;
- normal fast-forward only;
- correct F1–F4 and focused regressions;
- no redesign or new source PR.

Primary correction requirements are fully specified in packet `5854201198`.

The correction must prove canonical component-aware Worker containment, structural fail-closed condition inventory, lock-independent emergency recovery recording with Stop Now still killing exactly once, and dedicated descendant/orphan detection tests/mutations.

## 4. Review sequence

After the correction handoff/release, use ONE focused exact-head delta rereview of F1–F4.

Prefer a reviewer who is neither the Sol implementer nor the PR #23 design author. Prior integrated-review conclusions for unchanged code are retained.

No full design/source review restart unless the correction changes architecture.

## 5. Safety

ARCHIVE/CHECKPOINT FIRST.

G1 and GRL-015 remain accepted.
Office runner `grl-office` ID 3 and its real application/root/.env remain untouched.

No live jobs, Stop Now/reboot, private/control repo writes, App permission, personal enrollment, hosted Actions, service/autostart/security/sleep changes, global auth/env reset, destructive cleanup or credential exposure.

END_OF_GRL_HANDOFF key=GRL-20260927-PR24-F1-F4-CORRECTION-V53 sections=5