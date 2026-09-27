# CURRENT — github-runner-local

Updated: 2026-09-27. Phase: **GRL014_SOURCE_MERGED; A1_ID3_RETRY_AUTHORIZED; A5_ODG_GATED; GRL015_ACCEPTED**.

## Authority

- Canonical branch: main; Control Tower coordinates on Issue #1.
- Handoff: `docs/control-tower/handoffs/GRL-20260927-A1-ID3-INHERITED-ACL-RETRY-V59.md`.
- Sentinel: `END_OF_GRL_HANDOFF key=GRL-20260927-A1-ID3-INHERITED-ACL-RETRY-V59 sections=5`.
- Active task: retry owner-authorized A1 live witness on existing office runner ID3.
- Active packet: Issue #18 **`5856442213`**, FULL through `END_OF_GRL014_A1_RETRY_PACKET key=GRL014-A1-ID3-INHERITED-ACL-RETRY-20260927 sections=6`.

## Prior blocked attempt

Issue #18 `5856233967`: checkpoint ACL customization failed before mutation; zero witness requests; ID3/root/.env/Listener unchanged.

## Retry rule

Use a NEW owner-controlled checkpoint directory with EXISTING inherited permissions unchanged. Checkpoint validity comes from exact byte/hash/existence/attribute capture plus successful read-back verification.

Do NOT use Set-Acl, icacls, change ownership, repair domain trust or alter security policy.

Fresh local Windows Opus is the preferred operator.

After checkpoint PASS, previous A1 live scope remains: same ID3, temporary hooks/.env, idle Listener restart, exactly two js-smoke requests PASS then REFUSE-execute, byte-exact restore.

## Safety

A5/OD-G, personal enrollment and cross-machine switching remain unauthorized.
No re-registration/removal, reboot, workflow/profile mutation, third request or hosted Actions.
ARCHIVE/CHECKPOINT FIRST.