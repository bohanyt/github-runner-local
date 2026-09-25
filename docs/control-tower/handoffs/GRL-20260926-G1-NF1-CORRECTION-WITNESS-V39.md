# github-runner-local — live G1 partial success; NF-1 correction (V39)

## 1. Phase and authority

**G1_OFFICE_NEEDS_FIX; NF1_E_CORRECTION_READY; LIVE_RETRY_GATED.** Active task remains Issue #15, now correction packet `5841236994`, to be read IN FULL through `END_OF_GRL_G1_CORRECTION_PACKET key=GRL-G1-NF1-INPUT-ENV-AND-MISMATCH-20260926 sections=6`. It supersedes V38's immediate live-continuation instruction while the source defect is open, not the product goal or G1 criteria. CT claim: Issue #1 `5841229371`; implementer must observe CT release and claim independently.

Prior main `bc043fa1e4d2e404e36205dbace35af9af809318` and V38 remain the recoverable documentation checkpoint. No existing evidence/history is deleted. D remains merged at `5c268de282379a8dcd3a9165424d369ce804c5fa`, reviewed head `82f1722902e48635bc5e2a55c77a274a1e6e29cc`; no D campaign is reopened.

## 2. Actual progress and evidence limits

Operator live report Issue #15 `5841173894`, claim/release Issue #1 `5841016253` / `5841175436`: private repo bootstrap `3ca0449e39fdc28cf5ca15b30967833a95649192`, matching reviewed template tree `a5bbacdd434329c9346b452af32bb8e577839a2f`; mailbox #1; variables mailbox=1, authorized actors=[bohanyt], allowed branches=[main], disk reserve=10 GiB (operator measured about 34.7 GiB free). Reported real product Device Flow/account/private repo and registration-token/enrollment success establish the App API path; App settings UI was not independently inspected.

Product-enrolled `grl-office` ID 2, sole matching runner, version 2.337.0 with auto-update enabled, was online under the open wizard at operator release. Do not assert ongoing liveness from that snapshot. Keep existing registration/root/wizard and fresh-check before later live work.

Positive request comment `5841141808`, run `36201953082`, failed before ACK: admit job `108290306302` raised MISSING_ACTION_INPUT despite supplied inputs; execute/report skipped, no profile execution/result. CT fetched that job log and independently confirmed the source mismatch. CT also fetched prefilter-negative run `36202109916` job summaries: all four skipped. No checkout-mismatch live proof exists. G1 is NOT PASS. This CT did not perform the operator's local Windows work.

## 3. Correction implementer and minimal proof

SAME local Opus session may become ONE correction implementer, no subagents. Under a new Issue #1 claim after CT release, create/reuse the legitimately owned `fix/g1-action-input-env` branch and one new DRAFT PR from current main; do not reopen merged PR #17. Fix only production `templates/execution-repo/lib/action-io.mjs`: required/optional helpers must use runner naming (uppercase, ASCII spaces to underscores, hyphens preserved). No underscore alias, input renaming, new dependency or unrelated semantics.

Add real-environment contract regressions under template tests, covering all four actions' input names, required/optional/decoded behavior, absent/empty values, space conversion and hyphen-versus-underscore decoys. Demonstrate old-helper red/new-helper green. Use isolated test environments; no persistent env mutations or credentials. One final template Node suite plus existing lint/fixture/schema checks and targeted witness-artifact tests suffice; do not rerun .NET 384/384, WPF, D review or CLI probe. Live log reports Node 24; record test runtime honestly.

## 4. CT-selected mismatch witness preparation

Packet `5841236994` chooses a review-gated, disclosed acceptance-only workflow overlay. Prepare minimal deterministic overlay/rollback tooling or a complete executable runbook under `tools/g1-witness/**` and/or `docs/dev/G1-SHA-MISMATCH-WITNESS.md`, in the SAME PR. No active public workflow and no shipping override in the normal template.

After real admission, change only the execute job's grl-target checkout ref to another fixed harmless same-repo commit; add a narrow predeclared one-run request-nonce admission fence without removing existing mailbox/OWNER/actor guards. Preserve admitted requested identity, trusted checkout, profile and all production action code/pins/permissions. The real git HEAD must differ, and unchanged run-profile/report/verdict must produce the refusal themselves. Use distinct positive/negative requested SHAs to avoid status collision. Do not mock getHead, patch installed code or use global hooks.

Offline tests must check permitted delta, invalid/equal SHA refusal, baseline drift refusal, retained guards/pins/permissions and exact restoration. Later live use requires independent review and separate CT resume gate; it is NOT authorized now. Preserve pre-overlay commit/workflow blob, use normal commits, no other requests while overlay is active, then restore exact normal bytes and verify no override remains. Fresh disposable outcome evidence prevents stale result artifacts. Label the proof FAULT_INJECTED, not a naturally occurring checkout error. Full details and safeguards are in the packet.

## 5. Sequence and next gate

**SEQUENTIAL:** same-session correction implementation/tests/DRAFT PR -> ONE different independent delta reviewer (fix and witness method together) -> CT merge/resume decision -> same registered office runner live continuation. No local subagents, no deployment during review, no self-review. The independent gate is not another full D audit. The worker posts exact-head handoff on Issue #15 and releases its claim when source work is ready.

After the gate, preserve private main/settings before updating to reviewed corrected template; retain mailbox/variables/registration. A new request uses corrected workflow rather than rerunning the old failing SHA. Obtain normal positive proof, required prefilter evidence and separately disclosed mismatch refusal plus overlay restoration. Never claim G1 without all required live evidence. Existing-root reopening/restart is still a separate post-G1 priority before Issue #18 personal switching.

## 6. Deferred issue, safety and current hold

NF-2 is DEFERRED post-G1 D robustness: operator reported NoDefaultCurrentDirectoryInExePath interfered with cmd current-directory lookup on the first install. It does not justify a new D correction now; runner is already enrolled. First-attempt root and exploratory annotated tag remain preserved. Prior child-environment workaround is not permission to disable global or sandbox security settings.

Maintain present wizard/runner state; no new mailbox requests, automatic Stop Now, close/relaunch, reboot, unregister, deletion or second registration. Idle is not safe-drain proof. Owner may separately request explicit warned Stop Now. CT has not started monitoring or a background worker.

ARCHIVE/CHECKPOINT FIRST for persistent changes; secure local handling of any secrets, never public dumps. No global env/auth/credential resets, forced branch rewrites or deletion to start clean. No hosted Actions, service/UAC/security/sleep-policy changes, Stage-2/cross-repo execution or personal enrollment. Only complete future live proof permits G1_OFFICE_PASS.

END_OF_GRL_HANDOFF key=GRL-20260926-G1-NF1-CORRECTION-WITNESS-V39 sections=6
