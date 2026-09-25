# github-runner-local — D correction required, office G1 gated (V29)

## 1. Phase

**D_CORRECTION_READY; OFFICE_G1_GATED.**

Independent exact-head review found two blocking defects in DRAFT PR #17. There is no D merge authority or live G1 readiness.

## 2. Exact authority

Public main at this dispatch: `c38cae9a81fa81190cda6eeb5edbdea73356f6c9`.

Issue #16 is the D source task. SAME branch `feat/grl-d-portable-lifecycle`, SAME DRAFT PR #17, reviewed head `b14ff5fa108a4a1891efbc03f700bbffaaa3cfcf`. PR remained open/draft/unmerged at CT check. Source base at publication was `be6a34872db8c059de060fafbb4188a0e39e1ce1`; later main changes were continuity-only.

## 3. Independent result and evidence

Issue #16 review `5828083061`: **NEEDS_D_CORRECTION**, findings **R-D-1** and **R-D-2**. Independent reviewer claim was released at Issue #1 `5828088332`.

Reviewer independently reran Windows restore/build and two full solution test runs: 352/352 each, zero failed/skipped, build 0 warnings/errors. Independent deterministic harness reproduced both defects. The separate runner contract probe executable built but stalled downloading; a separate SHA-verified official runner v2.337.0 installer and production-adapter read-only CLI witness passed. This remains `LOCAL_CHECKED`, not live `WINDOWS_TESTED`.

## 4. Findings

R-D-1: Drain uses absent/idle remote status as permission to kill the owned process tree. A new job can be assigned after the snapshot; unregister follows the same path. Drain cannot claim to avoid killing an active job without a proven admission fence/cooperative stop and exact runner identity binding.

R-D-2: After a partial configure/start/online failure, the live wizard can show `INSTALL_CONFIGURE_FAILED` without a cleanup action and may close with a registered runner or installed root left behind. A recover/unregister path must remain usable without an owned runner process.

The complete exact locations, consequences and reproductions are in review `5828083061`.

## 5. Correction packet

Issue #16 CT packet `5828348757` defines a bounded correction for R-D-1/R-D-2 on the SAME branch and SAME DRAFT PR #17, starting at reviewed head `b14ff5fa108a4a1891efbc03f700bbffaaa3cfcf`.

ONE implementation worker claims, corrects/tests only within Issue #16's source/test allowlist, publishes one Issue #16 correction handoff and releases. If a supported safe Drain cannot be established, worker must stop with `D_CORRECTION_BLOCKED_RD1` and return the contract question to CT; no simulated success or quiet semantic downgrade.

A different independent reviewer will rereview a new exact head after a review-ready handoff.

## 6. Owner-approved live topology

OD-1/D09: private execution repo `bohanyt/github-runner-local-exec`.
OD-2/D10: owner-only Device Flow App; runtime Client ID on Issue #14 and installation settings owner-attested.
OD-7/D29: current office Windows laptop approved for first portable unelevated trusted-code-only runner `grl-office`.
D30: after G1 PASS only, personal runner `grl-personal` may be enrolled separately. Exactly ONE matching `grl-exec` runner process active at a time; no credential copying.

## 7. Live and security boundaries

Issue #15 office G1 remains GATED until D correction, independent PASS and a separate merge gate. No live login, private template activation, runner registration/start, workflow dispatch, hosted Actions, Stage-2/cross-repo credentials, service/UAC/elevation, personal enrollment before G1 PASS, or signing/release under the source correction packet.

A passing build does not resolve the two reproduced defects.

## 8. Next

Dispatch ONE bounded D correction worker using Issue #16 packet `5828348757`. CT fresh-checks claim/head before later publication and handles any blocked contract question. No merge or Issue #15 execution.

END_OF_GRL_HANDOFF key=GRL-20260925-D-CORRECTION-RD1-RD2-G1-GATED-V29 sections=8
