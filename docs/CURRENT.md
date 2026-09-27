# CURRENT — github-runner-local

Updated: 2026-09-28. Phase: **GRL014_SOURCE_MERGED; A1_TEMP_NO_EXTERNAL_ACTIONS_OWNER_DECISION; A5_ODG_GATED; GRL015_ACCEPTED**.

## Authority

- Canonical branch: main; Control Tower coordinates on Issue #1.
- Handoff: `docs/control-tower/handoffs/GRL-20260928-A1-NO-CODELOAD-PROPOSAL-V64.md`.
- Sentinel: `END_OF_GRL_HANDOFF key=GRL-20260928-A1-NO-CODELOAD-PROPOSAL-V64 sections=5`.
- Active proposal: Issue #18 **`5860607984`**, FULL through `END_OF_GRL014_A1_NO_CODELOAD_PROPOSAL key=GRL014-A1-TEMP-NO-EXTERNAL-ACTIONS-20260928 sections=6`.

## Diagnosis

Runner laptop and second device on the same office network both reproduce intermittent codeload mid-body stalls while raw.githubusercontent.com control is healthy. The failure is therefore not unique to the runner laptop. No specific company policy/security product/router/ISP/codeload cause has been proved.

Owner reports the second device cannot use an independent network, so that A/B path is unavailable.

## Proposed A1 path

Propose a temporary private execution-repo workflow with zero external `uses:` actions and one harmless built-in shell marker step. This decouples the remaining A1 proof from codeload/action downloads.

PASS: hook exit 0 then marker step executes.
REFUSE: execute/witness hook exit 73 and marker step does not execute.

No execution is authorized yet.

## Owner decision

Approval token:
`APPROVE_TEMP_NO_EXTERNAL_ACTIONS_A1_WITNESS`

## Safety

Office ID3, `.env`, product and private workflow remain untouched until approval. A5/OD-G/personal enrollment/cross-machine switching remain unauthorized.