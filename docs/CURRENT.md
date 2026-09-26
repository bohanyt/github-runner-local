# CURRENT — github-runner-local

Updated: 2026-09-26. Phase: **NF1_SUBSET_MERGED; NORMAL_POSITIVE_RESUME_READY; WITNESS_S1_GATED; G1_INCOMPLETE**.

## Authority

- Canonical branch: main; continuing owner-designated Control Tower coordinates on Issue #1.
- Handoff: `docs/control-tower/handoffs/GRL-20260926-NF1-ISOLATED-NORMAL-POSITIVE-V43.md`.
- Sentinel: `END_OF_GRL_HANDOFF key=GRL-20260926-NF1-ISOLATED-NORMAL-POSITIVE-V43 sections=4`.
- Active Issue #15 isolation/resume packet **`5841766071`**, FULL through `END_OF_GRL_NF1_ISOLATION_PACKET key=GRL-NF1-REVIEWED-SUBSET-NORMAL-POSITIVE-20260926 sections=4`.

## NF-1 merged; unsafe witness excluded

Independent closure review `5841741557` / release `5841743418`: original W-1 interruption and false-normal failures CLOSED offline; S-1 SAFETY_BLOCKER remains in the runbook's unchecked first anchor commit/push. NF-1 PASS retained. CT now separates the deployable fix from the unavailable harness, rather than another expanding tool rewrite.

PR #20 **MERGED** at `1aa9a91695029bc461b0e38ab180dc3ee8959a5d`, selecting original implementer commit `7bc4cecfc55465c5dc8b16bf474922bd53956707` unchanged. CT gate `5841772198`. Only template `lib/action-io.mjs` and `tests/action-io.test.mjs`; corrected template tree `4855ddca0143de2e0e21d95946b0e1b465b35143` matches the separately reviewed template exactly. Selected-commit-to-merge changes are docs only. NF-1 independent component acceptance is reused, not a new CT self-review or test run.

PR #19 remains draft/unmerged at `f911e7be18d7c574947416609be4c797c360eada`, history untouched; its three witness files are NOT in merged main. S-1 is not waived. No anchor/overlay/fault injection or harness execution authorized. Historical integrated template 117/118 (WRITE_SCOPE only), NF-1 10/10 and witness 22/22 remain attributed to prior runs, not relabelled all-green/newly run.

## Immediate next: SAME local Opus, one normal positive

After CT claim `5841762086` release/no competing operator, take a fresh Issue #1 claim and run the normal-positive operation under `5841766071`. Reuse existing grl-office ID 2, wizard/root, mailbox #1/four variables. Fresh-check actual ownership/online/version/non-elevation/queue/private repo/disk; do not reinstall or adopt unknown state.

Execute correct-private-destination/branch and clean index/worktree checks BEFORE any private commit/push, including effective fetch/push URLs. Checkpoint current private refs/tree/settings, apply only the reviewed two-file template delta with exact pre-state checks, normal fast-forward and fresh actual-remote verification. Final private root tree must be `4855ddca0143de2e0e21d95946b0e1b465b35143`. Last-known private main `3ca0449e39fdc28cf5ca15b30967833a95649192` is not authority to overwrite drift.

Submit ONE new ordinary harmless same-repo js-smoke request at the corrected SHA and follow ACK/execution/result/status/verdict to completion. Preserve exact evidence; publish promptly on Issue #15 and release claim. Successful positive = **NORMAL_POSITIVE_PASS / G1_INCOMPLETE**, not G1_OFFICE_PASS. Required mismatch/refusal proof remains gated. No further harness implementation or generic review/test loop before trying the accepted normal path; no .NET/WPF/384-test/probe rerun for unchanged code.

**SEQUENTIAL:** isolated merge completed -> SAME local normal-positive deployment/run/report -> later narrow S-1 witness closure and remaining G1 evidence. No subagents, no parallel private mutation. Historical prefilter `36202109916` stays historical. Full G1 and personal enrollment are still gated; do not mark the whole goal finished at positive-only progress.

## Safety

Preserve wizard/root/registration/private history/settings/evidence. No implicit Stop Now/unregister/close-relaunch/reboot/reinstall or idle-based safety assumption. No hosted Actions, service/UAC/security/sleep-policy changes, arbitrary/cross-repo/Stage-2/personal jobs or global env/auth resets. ARCHIVE/CHECKPOINT FIRST; pre-merge main `184e3a87c21d21ba62b04dcbc462ed530a706f86`/V42 and all original PR commits remain recoverable. Keep secrets/machine details private. NF-2/reopen/restart and Issue #18 switching stay later. CT has not deployed privately, run live work, or launched a background agent/monitor.
