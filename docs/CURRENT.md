# CURRENT — github-runner-local

Updated: 2026-09-28. Phase: **GRL014_SOURCE_MERGED; SECOND_DEVICE_REPRODUCES_REPORTED; A1_NETWORK_BLOCKED; OWNER_AFK_HOLD; A5_ODG_GATED; GRL015_ACCEPTED**.

## Authority

- Canonical branch: main; Control Tower coordinates on Issue #1.
- Handoff: `docs/control-tower/handoffs/GRL-20260928-SECOND-DEVICE-REPRODUCES-AFK-V63.md`.
- Sentinel: `END_OF_GRL_HANDOFF key=GRL-20260928-SECOND-DEVICE-REPRODUCES-AFK-V63 sections=5`.
- CT evidence record: Issue #18 **`5858205161`**, through `END_OF_GRL_HANDOFF key=GRL014-SECOND-DEVICE-REPRODUCES-OWNER-AFK-20260928 sections=5`.

## Second-device result

The owner requested the local-only diagnostic, confirmed same office Wi-Fi/SSID in chat, and forwarded its result. V62's pre-test approval pointer is superseded by this record; no retroactive worker claim or formal approval-token comment is invented.

Owner-forwarded LOCAL_CHECKED report: codeload 6/10 complete, 4/10 mid-body HTTP-200/curl-28 timeouts; raw control 10/10 complete. One completed codeload transfer took 74.395 seconds. Reported hash consistency and unchanged network context are not independently re-measured by CT; raw scratch evidence has not been inspected.

Classification retained: **SECOND_DEVICE_REPRODUCES**. Shared office-network/upstream codeload path is a stronger hypothesis than a problem unique to the runner laptop. No specific policy, security software or infrastructure cause is established.

## Owner AFK hold

The owner is AFK/asleep. No new worker, A1 retry, GRL request, hook/.env mutation, product/runner stop or restart, network switch, cache seed, security/ACL change or unattended polling is authorized by this update.

Leave office ID 3 untouched; its last accepted restored online/idle state is from `5857287220`, not a fresh CT observation. Preserve all checkpoint/proof files, worktrees and device-2 scratch evidence.

## Retained progress and next decision

G1, GRL-015 and PR #24 source acceptance remain complete. A1 execute PASS/profile and execute refusal remain pending; A5/OD-G and personal enrollment remain gated.

After owner return, proposed next discriminator is the same second device and existing bounded probe on an owner-approved independent connection, leaving the runner laptop untouched. Not dispatched now. No need to repeat another long same-network/protocol campaign or source-review loop.

Do not ask the sleeping owner for more clicks or approval tokens. No live or scheduled operation has been started by this CT publication.
