# CURRENT — github-runner-local

Updated: 2026-09-26. Phase: **G1_OFFICE_NEEDS_FIX; NF1_E_CORRECTION_READY; LIVE_RETRY_GATED**.

## Authority

- Canonical branch: `main`; continuing owner-designated Control Tower coordinates on Issue #1.
- Current handoff: `docs/control-tower/handoffs/GRL-20260926-G1-NF1-CORRECTION-WITNESS-V39.md`.
- Required sentinel: `END_OF_GRL_HANDOFF key=GRL-20260926-G1-NF1-CORRECTION-WITNESS-V39 sections=6`.
- Active task: **Issue #15 correction packet `5841236994`**, FULL through `END_OF_GRL_G1_CORRECTION_PACKET key=GRL-G1-NF1-INPUT-ENV-AND-MISMATCH-20260926 sections=6`.

## Live progress, not full G1 PASS

Operator report `5841173894`, claim release `5841175436`: real product login/private-repo confirmation/enrollment succeeded. `grl-office` ID 2 was online, sole matching runner, version 2.337.0, under the open wizard at release. Private bootstrap `3ca0449e39fdc28cf5ca15b30967833a95649192` matches reviewed template tree `a5bbacdd434329c9346b452af32bb8e577839a2f`; mailbox #1 and FOUR variables already set (reserve 10 GiB). Preserve all of this; fresh-check runtime state before eventual retry, do not reinstall/register again.

Positive run `36201953082` failed at admission before ACK with MISSING_ACTION_INPUT. CT fetched admit job `108290306302` logs and source: NF-1 helper replaces hyphens with underscores but official runner v2.337.0 preserves hyphens. Prefilter negative `36202109916` all four jobs skipped (CT fetched summaries). Real checkout-mismatch witness remains missing. Only the required complete live evidence may become G1_OFFICE_PASS; CT did not personally execute the Windows operator work.

## Immediate implementation scope

After CT claim `5841229371` release/no competitor, the SAME local Opus may take a NEW correction claim and implement on `fix/g1-action-input-env`, one new DRAFT PR from current main (reuse a legitimately existing matching branch/PR; do not reopen merged #17). No subagents. Production edit limited to `templates/execution-repo/lib/action-io.mjs`; tests under template tests use actual runner input environment names and cover all four actions. Prepare the complete deterministic acceptance-only mismatch overlay/rollback under `tools/g1-witness/**` and/or `docs/dev/G1-SHA-MISMATCH-WITNESS.md` for review in the same PR; normal production workflow stays unchanged.

CT-selected witness changes only the test run's grl-target checkout to another fixed harmless same-repo commit plus an additional one-run request-nonce admission fence. Preserve requested identity, trusted action code, existing guards, pins and permissions; no mocks or installed runner hooks. Test the minimal delta and exact restoration offline. Live activation of the overlay is still review/merge/resume-gated. See packet for identity separation, backup, isolation and rollback requirements.

## Sequence and limits

**SEQUENTIAL:** implement/test/DRAFT PR -> ONE independent exact-delta review of fix and witness artifacts -> CT merge/resume gate -> same office registration live continuation. Worker releases its implementation claim at handoff; no self-review, private deployment or retry before the gate. Resolve normal implementation/tooling issues within the same session.

D remains reviewed/merged (`5c268de282379a8dcd3a9165424d369ce804c5fa`; reviewed `82f1722902e48635bc5e2a55c77a274a1e6e29cc`). Do NOT repeat D review, WPF, two-pass 384/384 or RunnerContractProbe for this JS correction. Targeted red/green regression + one final complete template Node suite and existing lint/fixture/schema/overlay checks, then live continuation after review. NF-2 cmd lookup / NoDefaultCurrentDirectoryInExePath is DEFERRED post-G1 robustness, not this correction.

## Standing safety

Preserve current wizard/runner/root/registration; no new mailbox jobs while gated, no implicit Stop Now/unregister/close-relaunch/reboot or idle-based cleanup. No hosted Actions, service/UAC/security/sleep-policy changes, Stage-2/cross-repo or personal runner. No global auth/env resets. ARCHIVE/CHECKPOINT FIRST before persistent changes; previous main `bc043fa1e4d2e404e36205dbace35af9af809318` and V38 remain recoverable history. Keep secrets and local machine details out of public evidence. After accepted G1, prioritize restart/reopen and then separate Issue #18 one-active-at-a-time switching.
