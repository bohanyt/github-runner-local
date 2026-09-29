# CURRENT — github-runner-local

Updated: 2026-09-29. Phase: **GRL014_SOURCE_MERGED; REAL_DOGFOOD_FIRST; A1_PENDING; ACTION_CACHE_ESCAPE_HATCH_ONLY; A5_ODG_GATED; GRL015_ACCEPTED**.

## Authority

- Canonical branch: `main`; Control Tower coordinates on Issue #1.
- Handoff: `docs/control-tower/handoffs/GRL-20260929-REAL-DOGFOOD-FIRST-V65.md`.
- Sentinel: `END_OF_GRL_HANDOFF key=GRL-20260929-REAL-DOGFOOD-FIRST-V65 sections=8`.
- Active CT adjudication: Issue #18 **`5881385625`**, through `END_OF_GRL014_REAL_DOGFOOD_ADJUDICATION key=GRL014-REAL-DOGFOOD-FIRST-20260929 sections=6`.

## Product direction

Owner prefers real cross-project dogfood over more synthetic proof and does not want GRL overengineered around the office `codeload.github.com` anomaly.

V64 temporary no-external-actions A1 witness proposal remains PARKED as a fallback; it is not authorized for execution.

## Retained diagnosis

Runner laptop and a second device on the same office network both reproduced intermittent HTTP-200 mid-body codeload stalls while raw.githubusercontent.com controls stayed healthy. This points away from a runner-laptop-only issue but does not prove a specific policy/root cause. Independent-network A/B is unavailable.

Native `ACTIONS_RUNNER_ACTION_ARCHIVE_CACHE` support is source-verified as a possible escape hatch. Owner-forwarded Opus scratch work found Git-fetch/git-archive provisioning viable, but no live cache acceptance exists. Do NOT productize or install it unless real dogfood demonstrates the need.

## Real dogfood candidates

- KIN Marketing/KIN MCP: **NOT DOGFOOD_READY**; no authorized executable repo/branch/exact SHA yet.
- Recantor: closest real Windows candidate. R63-C2 is CLEAN and local Windows updater pre-witness passed, but final normal release-distribution acceptance remains pending. Fresh-check Recantor authority before using it; do not duplicate/bypass its acceptance work.

## Next

**SEQUENTIAL:** wait for/fresh-check one truthful bounded real Windows workload contract -> derive smallest fixed GRL profile/contract -> owner approval -> checkpoint -> run one real trusted non-destructive job -> use its result to decide whether codeload cache mitigation is actually necessary.

Do not manufacture a workload if no project is ready.

## Safety

A1 remains pending. A5/OD-G, personal enrollment and cross-machine switching remain unauthorized. No GRL request, execution-hub mutation, cache install, ID3/root/.env/product change, network/security change, or hosted Actions dispatch is authorized by this CURRENT alone.

ARCHIVE/CHECKPOINT FIRST before any later persistent/destructive action.