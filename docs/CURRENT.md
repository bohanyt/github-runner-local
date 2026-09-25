# CURRENT — github-runner-local

Updated: 2026-09-25. Phase: **D_FAIL_CLOSED_CORRECTION_READY; OFFICE_G1_GATED**.

## Authority

- Canonical branch: `main`.
- Control Tower: Issue #1, owner-designated successor CT.
- Current handoff: `docs/control-tower/handoffs/GRL-20260925-D-FAIL-CLOSED-CORRECTION-G1-GATED-V30.md`.
- Required sentinel: `END_OF_GRL_HANDOFF key=GRL-20260925-D-FAIL-CLOSED-CORRECTION-G1-GATED-V30 sections=8`.

## Source state

C and E are independently review-clean and merged (C `451d3ab889f9f63f45ebf90ba54ddece7b60e6eb`; E `a296f3f9dfa362f6a53d20ff81a46eef7a519819`).

D source is **NOT merged**. DRAFT PR #17 branch `feat/grl-d-portable-lifecycle` remains at `b14ff5fa108a4a1891efbc03f700bbffaaa3cfcf`; original D source base `be6a34872db8c059de060fafbb4188a0e39e1ce1`. The independent review Issue #16 `5828083061` found R-D-1 unsafe Drain/unregister assignment race and R-D-2 inaccessible partial-configure cleanup. Both are still open. Windows build/tests at the unchanged head passed 352/352 twice, but reproduced races remain; evidence `LOCAL_CHECKED`.

## CT technical decision

Blocked correction handoff Issue #16 `5828419459` established that pinned runner v2.337.0 Ctrl+C requests shutdown and can cancel an active job. No supported safe automatic Drain is proved inside the reviewed portable CLI contract. Decision D31: D source must fail closed, with no automatic drain/stop or implicit unregister from remote idle/absent snapshots. Distinct explicit Stop Now warns of job cancellation. D32 / Issue #18: admission fence and safe office/personal switch are OPEN, not accepted.

## Active task: revised bounded correction

Issue #16 CT packet `5828534853` plus product-gap addendum `5828855949` govern ONE worker on the SAME branch/PR from the unchanged head and within the original D source/test allowlist. Correct R-D-1 by disabling unsafe automatic Drain/implicit stop, including register/resume failure catches; correct R-D-2 with accessible live recovery and bounded persisted non-secret state on reopen; supply E-required runner version/identity metadata at child start; show truthful live UI. Run every compatible cloud/Linux deterministic check, report exact limits, publish a `WINDOWS_PENDING` source handoff and release. Issue #16 sequencing addendum `5829054146` records D33: no cloud Windows environment is available. DIFFERENT reviewer may source-rereview the exact head; before merge, a separate read-only build/tests/CLI proof must run on the current office Windows laptop. Only after merge may Issue #15 live G1 run. Advisory product audit Issue #1 `5828798876` is source-only; it is not a G1 or review PASS.

## Owner-approved live topology and gate

OD-1/D09 private execution repo `bohanyt/github-runner-local-exec`; OD-2/D10 owner-only Device Flow App; OD-7/D29 office Windows laptop `grl-office` approved for first portable unelevated trusted-code-only G1. D30 conditionally authorizes `grl-personal` after G1 PASS with one matching `grl-exec` runner active at a time.

Issue #15 G1 remains GATED until source correction, independent exact-head PASS and separate CT/owner merge. Clarifications `5828537511` and `5828862963` prevent implicit Stop Now/unregister, require all four E template variables, local Git/App permission checks and one staged office G1 runbook. The private execution repo still has only README and no active workflow. G1 does not prove a safe two-laptop switch; Issue #18 plus addendum `5828869070` tracks a request-level admission fence and tests the server-side DELETE/422+self-exit hypothesis separately. D30 remains conditional; neither server behavior nor personal activation is approved as witnessed.

## Standing boundaries

No GitHub-hosted Actions; live login/runner registration/start or private template activation under D source work; Stage-2/cross-repo credentials or workload; public App distribution; service/UAC/elevated helper; arbitrary shell inbox; personal activation without G1 PASS and a proved one-active-at-a-time procedure; signing/release.

## Next

Dispatch one bounded cloud/Linux D correction worker using Issue #16 `5828534853`, product addendum `5828855949` and no-cloud-Windows sequencing `5829054146` on the same draft PR. CT then arranges independent exact-head source review, office-laptop read-only Windows proof, merge gate and G1. No extra planning round before source correction.
