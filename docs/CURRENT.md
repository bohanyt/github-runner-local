# CURRENT — github-runner-local

Updated: 2026-09-27. Phase: **GRL014_SOURCE_MERGED; A1_NETWORK_DIAG_REQUIRED; A5_ODG_GATED; GRL015_ACCEPTED**.

## Authority

- Canonical branch: main; Control Tower coordinates on Issue #1.
- Handoff: `docs/control-tower/handoffs/GRL-20260927-A1-CODELOAD-DIAG-V61.md`.
- Sentinel: `END_OF_GRL_HANDOFF key=GRL-20260927-A1-CODELOAD-DIAG-V61 sections=5`.
- Active task: diagnose intermittent `codeload.github.com` action archive transfer before any further A1 retry.
- Active packet: Issue #18 **`5857315791`**, FULL through `END_OF_GRL014_NETWORK_DIAG_PACKET key=GRL014-A1-CODELOAD-DIAG-20260927 sections=6`.

## State

Latest A1 retry Issue #18 `5857287220` blocked at the mandatory post-restart network gate. No witness request was posted; rollback passed; same ID3 is online/idle at baseline.

Retain prior partial A1 real integration evidence. Execute PASS/profile and execute refusal remain pending.

## Diagnostic rule

No A1 retry is authorized right now.

Network diagnosis is scratch/read-only only: repeated exact codeload GETs, per-process HTTP/1.1/IPv4 comparisons, read-only proxy/DNS/TLS observations.

Do NOT mutate runner/root/.env, restart product, post GRL requests, seed action cache, change proxy/DNS/firewall/AV/EDR/domain/security settings, or switch the runner laptop network.

## Sequence

**SEQUENTIAL:** network diagnosis -> CT classification -> bounded next decision.

## Safety

A1 remains pending. A5/OD-G, personal enrollment and cross-machine switching remain unauthorized.
ARCHIVE/CHECKPOINT FIRST for any later live action.