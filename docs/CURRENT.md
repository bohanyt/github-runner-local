# CURRENT — github-runner-local

Updated: 2026-09-27. Phase: **GRL014_PR24_F1_F4_CORRECTION_READY; LIVE_ACTIVATION_GATED; GRL015_ACCEPTED**.

## Authority

- Canonical branch: main; Control Tower coordinates on Issue #1.
- Handoff: `docs/control-tower/handoffs/GRL-20260927-PR24-F1-F4-CORRECTION-V53.md`.
- Sentinel: `END_OF_GRL_HANDOFF key=GRL-20260927-PR24-F1-F4-CORRECTION-V53 sections=5`.
- Active task: Issue #18 / PR #24 focused F1–F4 correction.
- Active correction packet: Issue #18 comment **`5854201198`**, FULL through `END_OF_GRL014_CORRECTION_PACKET key=GRL014-PR24-F1-F4-20260927 sections=6`.

## Review disposition

Integrated review Issue #18 `5853961376` returned `NEEDS_GRL014_SOURCE_CORRECTION` at exact old PR #24 head `d0188b845d5cdc01542c4fc12cd63fce37ab3a2f`.

Accepted blockers:
- F1 SOURCE_SAFETY: orphaned same-runner Worker can be missed for non-canonical/trailing-separator runner path;
- F2 DESIGN_CONFORMANCE: lint misses equivalent unconditional `if` layouts;
- F3 DESIGN_CONFORMANCE: stale `gate.lock` disables warned Stop Now;
- F4 TEST_EVIDENCE: descendant/image detection rules mask one another in tests.

Reviewer claim `5853704407` released by `5853963616`.

## Correction

SAME Sol branch / SAME DRAFT PR #24, normal fast-forward only.

Correct only F1–F4 plus focused tests. Retain accepted integrated-review conclusions for unchanged source. Do not reopen PR #23 design research, A1/A5 live proof or unrelated nonblocking notes.

After correction handoff/release: ONE focused delta rereview, preferably by a reviewer who is neither Sol implementer nor design author.

## Safety

G1/GRL-015 remain accepted. Office ID 3 stays untouched.
No live hook/.env change, job, Stop Now/reboot, private/control repo mutation, App permission, personal enrollment or hosted Actions.
ARCHIVE/CHECKPOINT FIRST.