# CURRENT — github-runner-local

Updated: 2026-09-25. Phase: **D_CORRECTION_READY; OFFICE_G1_GATED**.

## Authority

- Canonical branch: `main`.
- Control Tower: Issue #1, owner-designated successor CT.
- Current handoff: `docs/control-tower/handoffs/GRL-20260925-D-CORRECTION-RD1-RD2-G1-GATED-V29.md`.
- Required sentinel: `END_OF_GRL_HANDOFF key=GRL-20260925-D-CORRECTION-RD1-RD2-G1-GATED-V29 sections=8`.

## Source state

C and E are independently review-clean and merged:
- C merge `451d3ab889f9f63f45ebf90ba54ddece7b60e6eb`.
- E merge `a296f3f9dfa362f6a53d20ff81a46eef7a519819`.

D source is **NOT merged**. DRAFT PR #17 is on branch `feat/grl-d-portable-lifecycle` at independently reviewed head `b14ff5fa108a4a1891efbc03f700bbffaaa3cfcf`. Its source base at publication was `be6a34872db8c059de060fafbb4188a0e39e1ce1`; later main authority-doc advancement is expected.

## Independent D result

Issue #16 review comment `5828083061`: **NEEDS_D_CORRECTION**, 2 blocking findings at that exact head:
- R-D-1: Drain/unregister may kill a job assigned after an absent/idle status snapshot.
- R-D-2: Partial configure failure can strand the live wizard without cleanup.

Reviewer release: Issue #1 comment `5828088332`.

Independent Windows build and two test runs passed 352/352 each; deterministic harness reproduced both defects. Separate runner probe download stalled; independently hash-verified production installer and read-only CLI adapter witness passed. Evidence remains `LOCAL_CHECKED`, not real G1/`WINDOWS_TESTED`.

## Active task: bounded correction

Issue #16 CT correction packet `5828348757` governs ONE worker on the SAME branch and SAME DRAFT PR #17, starting from `b14ff5fa108a4a1891efbc03f700bbffaaa3cfcf`. Fix only R-D-1/R-D-2 within Issue #16's source/test allowlist, run proof, push normal fast-forward, publish handoff, release and stop. No self-review or merge.

If safe cooperative Drain cannot be established within the reviewed runner contract, worker reports `D_CORRECTION_BLOCKED_RD1` to CT instead of declaring success. A different reviewer must rereview any corrected exact head.

## Owner-approved live topology

- OD-1/D09: dedicated private repo `bohanyt/github-runner-local-exec`.
- OD-2/D10: owner-only Device Flow App for acceptance; Client ID on Issue #14; installation settings owner-attested.
- OD-7/D29: current office Windows laptop approved for first portable unelevated trusted-code-only `grl-office`.
- D30: `grl-personal` only after G1 PASS, separate credentials and one active matching `grl-exec` runner at a time.

## Gated live task

Issue #15 office G1 remains GATED until D correction, independent exact-head PASS and separate CT/owner merge. No source test or read-only probe substitutes for real Device Flow, runner online and harmless same-repo workflow witness.

## Standing boundaries

No GitHub-hosted Actions; Stage-2/cross-repo credentials or workload; public App distribution; service/UAC/elevated helper; arbitrary shell inbox; personal enrollment before G1 PASS; signing/release.

## Next

Dispatch one bounded D correction worker from Issue #16 packet `5828348757`. CT handles a blocked design question or subsequent independent rereview.
