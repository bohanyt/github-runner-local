# CURRENT — github-runner-local

Updated: 2026-09-27. Phase: **GRL014_SOURCE_MERGED; A1_ID3_NETWORK_GATED_RETRY_AUTHORIZED; A5_ODG_GATED; GRL015_ACCEPTED**.

## Authority

- Canonical branch: main; Control Tower coordinates on Issue #1.
- Handoff: `docs/control-tower/handoffs/GRL-20260927-A1-ID3-NETWORK-GATED-RETRY-V60.md`.
- Sentinel: `END_OF_GRL_HANDOFF key=GRL-20260927-A1-ID3-NETWORK-GATED-RETRY-V60 sections=5`.
- Active task: network-gated successor retry of owner-authorized A1 live witness on existing office runner ID3.
- Active packet: Issue #18 **`5857096676`**, FULL through `END_OF_GRL014_A1_NETWORK_RETRY_PACKET key=GRL014-A1-ID3-NETWORK-GATED-RETRY-20260927 sections=6`.

## Prior attempt

Issue #18 `5857026595`: checkpoint/rollback PASS; request #1 cancelled before execute hook because codeload action download timed out. Request #2 not posted.

Retain real integration evidence for admit/report/verdict and admit hook-before-checkout ordering.

## Retry rule

Before any live mutation/request, exact pinned action archive downloads must pass two consecutive scratch-download rounds. After install/restart, repeat one round before request #1. Do not alter runner action cache or network/system settings.

Opus local Windows preferred. Product/wizard starts/stops/resumes are owner-performed from Explorer/desktop only, never from agent shell.

Same ID3, exactly two new js-smoke requests maximum if prerequisites hold, byte-exact restore.

## Safety

A5/OD-G, personal enrollment and cross-machine switching remain unauthorized.
No re-registration/removal, reboot, workflow/profile mutation, third request, proxy/DNS/firewall/domain/security changes or hosted Actions.
ARCHIVE/CHECKPOINT FIRST.