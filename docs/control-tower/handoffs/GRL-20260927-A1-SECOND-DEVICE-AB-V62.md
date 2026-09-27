# github-runner-local — A1 paused; second-device same-network A/B proposed (V62)

## 1. Phase

**GRL014_SOURCE_MERGED; A1_SECOND_DEVICE_AB_OWNER_DECISION; A5_ODG_GATED; GRL015_ACCEPTED.**

Accepted diagnosis: Issue #18 `5857817648` = INTERMITTENT_PATH_OR_INSPECTION.

Active proposal: Issue #18 `5857878590`, through:
`END_OF_GRL014_CODELOAD_AB_PROPOSAL key=GRL014-A1-SECOND-DEVICE-SAME-NETWORK-20260927 sections=6`.

## 2. Current diagnosis

Runner laptop shows intermittent codeload mid-body stalls after HTTP 200 across all three pinned action archives. Raw.githubusercontent.com control remains consistently healthy. No proxy/TLS inspection certificate was observed. Root cause remains unresolved between upstream/path and runner-laptop-specific factors.

## 3. Next discriminator

Proposed next step, NOT YET AUTHORIZED:
- second device on the same office network;
- 10 exact codeload checkout downloads + 10 interleaved raw control downloads;
- scratch/read-only only;
- runner laptop and ID3 untouched.

Owner approval token required: `APPROVE_SAME_NETWORK_SECOND_DEVICE_AB`.

## 4. A1 gate

A1 live witness remains paused. Do not restart/mutate runner ID3 for A1 while codeload instability is unresolved.

## 5. Safety

No runner/root/.env/product changes, no GRL requests, no cache seed, no network/system-setting changes, no A5/OD-G/personal enrollment.

END_OF_GRL_HANDOFF key=GRL-20260927-A1-SECOND-DEVICE-AB-V62 sections=5