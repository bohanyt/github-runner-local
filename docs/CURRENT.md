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

Issue #16 CT packet `5828534853` supersedes the prior correction packet where they conflict. ONE worker uses SAME branch/PR, starting from unchanged head, within the existing D source/test allowlist. Correct R-D-1 by disabling unsafe automatic Drain and implicit stop during unregister; correct R-D-2 with accessible live UI recovery and non-secret pending/cleanup path. Run deterministic race/recovery proof plus Windows restore/build/twice tests and read-only witness. Publish exact-head handoff and release. DIFFERENT independent reviewer must rereview; no merge yet.

## Owner-approved live topology and gate

OD-1/D09 private execution repo `bohanyt/github-runner-local-exec`; OD-2/D10 owner-only Device Flow App; OD-7/D29 office Windows laptop `grl-office` approved for first portable unelevated trusted-code-only G1. D30 conditionally authorizes `grl-personal` after G1 PASS with one matching `grl-exec` runner active at a time.

Issue #15 G1 remains GATED until source correction, independent exact-head PASS and separate CT/owner merge. Issue #15 clarification `5828537511` forbids implicit Stop Now/unregister on active process during failed enrollment. G1 execution does not prove a safe two-laptop switch; Issue #18 tracks that separate procedure.

## Standing boundaries

No GitHub-hosted Actions; live login/runner registration/start or private template activation under D source work; Stage-2/cross-repo credentials or workload; public App distribution; service/UAC/elevated helper; arbitrary shell inbox; personal activation without G1 PASS and a proved one-active-at-a-time procedure; signing/release.

## Next

Dispatch one bounded D fail-closed correction worker from Issue #16 `5828534853`. CT then arranges a different independent exact-head reviewer and handles a separate merge/G1 gate.
