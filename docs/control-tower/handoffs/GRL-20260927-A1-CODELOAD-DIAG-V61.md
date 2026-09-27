# github-runner-local — A1 paused for codeload diagnosis (V61)

## 1. Phase

**GRL014_SOURCE_MERGED; A1_NETWORK_DIAG_REQUIRED; A5_ODG_GATED; GRL015_ACCEPTED.**

Active diagnosis packet: Issue #18 `5857315791`, through:
`END_OF_GRL014_NETWORK_DIAG_PACKET key=GRL014-A1-CODELOAD-DIAG-20260927 sections=6`.

Latest A1 retry `5857287220` blocked before requests because post-restart checkout archive download stalled. Rollback passed; ID3 restored online/idle.

## 2. Retained A1 evidence

Retain partial real integration evidence from prior attempt: root `.env` -> pinned Listener -> pinned Worker -> started hook for admit/report/verdict, plus admit hook before checkout.

Execute PASS/profile and execute exit-73 refusal remain pending.

## 3. Network diagnosis

Do not retry A1 yet. Diagnose codeload behavior using scratch-only bounded GETs and read-only proxy/DNS/protocol observations.

No runner/root/.env/product restart/request/cache/network-setting mutation.

Current evidence does not prove a hard organization policy block because identical checkout downloads have both succeeded rapidly and stalled mid-body.

## 4. Sequence

**SEQUENTIAL:** network diagnosis -> CT classification -> optional bounded A/B or A1 retry decision.

## 5. Safety

Do not alter proxy/DNS/firewall/AV/EDR/domain/security settings. Do not switch the runner laptop network under this packet.
A1 remains pending. A5/OD-G/personal enrollment/cross-machine switching remain unauthorized.

END_OF_GRL_HANDOFF key=GRL-20260927-A1-CODELOAD-DIAG-V61 sections=5