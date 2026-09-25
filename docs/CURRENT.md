# CURRENT — github-runner-local

Updated: 2026-09-25. Phase: **D_INDEPENDENT_REVIEW_READY; OFFICE_G1_GATED**.

## Authority

- Canonical branch: `main`.
- Control Tower: Issue #1; owner-designated successor CT.
- Current handoff: `docs/control-tower/handoffs/GRL-20260925-D-INDEPENDENT-REVIEW-READY-G1-GATED-V28.md`.
- Required sentinel: `END_OF_GRL_HANDOFF key=GRL-20260925-D-INDEPENDENT-REVIEW-READY-G1-GATED-V28 sections=8`.

## Merged source

Checkpoint C and E are independently review-clean and merged:
- C merge `451d3ab889f9f63f45ebf90ba54ddece7b60e6eb`.
- E merge `a296f3f9dfa362f6a53d20ff81a46eef7a519819`.

Checkpoint D is NOT merged.

## Active task: independent D source review

Issue #16 / GRL-013 implementation worker published DRAFT PR #17 on branch `feat/grl-d-portable-lifecycle`, exact head `b14ff5fa108a4a1891efbc03f700bbffaaa3cfcf`. Its source base at publication was `be6a34872db8c059de060fafbb4188a0e39e1ce1`; continuity-only main advancement is expected.

- Worker handoff: Issue #16 comment `5827081178`.
- Worker claim release: Issue #1 comment `5827088164`.
- Independent review packet: Issue #16 comment `5827801807`.
- Base-SHA clarification: Issue #16 comment `5827814992`.
- Worker evidence: `LOCAL_CHECKED`, 352/352 in each of two full test runs, 0 failed/skipped, Windows read-only probes PASS. No live acceptance.

ONE different independent reviewer claims and assesses the entire exact-head PR, especially the drain/job-assignment race. Reviewer publishes one Issue #16 result, releases, and stops. CT coordinates corrections or a separate merge gate.

## Owner-approved live topology

OD-1/D09: dedicated private execution repo `bohanyt/github-runner-local-exec`.
OD-2/D10: owner-only Device Flow App, runtime Client ID on Issue #14, installation settings owner-attested.
OD-7/D29: current office Windows laptop approved for first portable unelevated runner `grl-office`.
D30: personal runner `grl-personal` only after G1 PASS; exactly ONE active `grl-exec` runner at a time, with separate registration and credentials.

## Gated live office G1

Issue #15 / GRL-012 remains **GATED** until D independent PASS, any corrections and a separate CT/owner merge. Then CT fresh-reads main and dispatches the real office-laptop witness. A source build or review does not prove real Device Flow, runner online, fixture execution or `WINDOWS_TESTED`.

## Standing boundaries

No GitHub-hosted Actions; Stage-2/cross-repo credentials or workload; public App distribution; service/UAC/elevated helper; arbitrary shell inbox; personal enrollment before G1 PASS; signing/release.

## Next

Dispatch ONE different independent reviewer for PR #17 using Issue #16 packet `5827801807` and clarification `5827814992`. No self-review, source edits, merge or Issue #15 execution.
