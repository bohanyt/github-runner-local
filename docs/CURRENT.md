# CURRENT — github-runner-local

Updated: 2026-09-27. Phase: **GRL014_SOURCE_MERGED; A1_SECOND_DEVICE_AB_OWNER_DECISION; A5_ODG_GATED; GRL015_ACCEPTED**.

## Authority

- Canonical branch: main; Control Tower coordinates on Issue #1.
- Handoff: `docs/control-tower/handoffs/GRL-20260927-A1-SECOND-DEVICE-AB-V62.md`.
- Sentinel: `END_OF_GRL_HANDOFF key=GRL-20260927-A1-SECOND-DEVICE-AB-V62 sections=5`.
- Active proposal: Issue #18 **`5857878590`**, FULL through `END_OF_GRL014_CODELOAD_AB_PROPOSAL key=GRL014-A1-SECOND-DEVICE-SAME-NETWORK-20260927 sections=6`.

## Accepted diagnosis

Issue #18 `5857817648`: INTERMITTENT_PATH_OR_INSPECTION.

Runner laptop codeload requests intermittently stall mid-body across all three pinned action archives. raw.githubusercontent.com control remains healthy. No hard policy block, proxy, or TLS interception has been proved.

## Next decision

Proposed next step requires owner approval token:
`APPROVE_SAME_NETWORK_SECOND_DEVICE_AB`

Test would use another device on the same office network for bounded scratch-only codeload vs raw control downloads. Runner laptop and ID3 remain untouched.

## A1

A1 live witness remains paused pending this discriminator or other CT decision.

## Safety

No runner/root/.env/product action, no GRL request, no cache seed, no proxy/DNS/firewall/security changes, no A5/OD-G/personal enrollment.