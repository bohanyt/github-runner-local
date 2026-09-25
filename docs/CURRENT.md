# CURRENT — github-runner-local

Updated: 2026-09-25. Phase: **D_SOURCE_IMPLEMENTATION_READY; OFFICE_G1_GATED**.

## Authority

- Canonical branch: `main`.
- Control Tower: Issue #1; owner-designated successor CT claim `5826687223`.
- Current handoff: `docs/control-tower/handoffs/GRL-20260925-D-SOURCE-PENDING-OFFICE-G1-GATED-V27.md`.
- Required sentinel: `END_OF_GRL_HANDOFF key=GRL-20260925-D-SOURCE-PENDING-OFFICE-G1-GATED-V27 sections=8`.

## Merged checkpoints

Checkpoint C and E are independently review-clean and merged:
- C merge: `451d3ab889f9f63f45ebf90ba54ddece7b60e6eb`.
- E merge: `a296f3f9dfa362f6a53d20ff81a46eef7a519819`.

Checkpoint B still composes fake preview adapters. Production runner lifecycle and live wizard integration are NOT merged.

## Owner-approved live target

- OD-1/D09: private execution repo `bohanyt/github-runner-local-exec`, private and default `main` as last verified.
- OD-2/D10: owner-only acceptance GitHub App, Device Flow enabled; runtime Client ID recorded on Issue #14; App installation settings owner-attested.
- OD-7/D29: current OFFICE WINDOWS LAPTOP is approved for the first portable, unelevated, trusted-code-only G1 runner `grl-office`.
- D30: only AFTER G1 PASS may the PERSONAL laptop be enrolled as `grl-personal`; if both share `grl-exec`, exactly ONE runner process is active at a time. Credentials are never copied.

These approvals remain valid; they do not prove that a live runner exists.

## Active task: Checkpoint D source

Issue #16 `GRL-013 — Checkpoint D source: portable runner lifecycle + live wizard adapters` is the active bounded source implementation task. Follow its full body and CT batch-launch clarification comment `5826696683`.

One implementation worker claims Issue #1, creates branch `feat/grl-d-portable-lifecycle`, implements within Issue #16's allowlist, performs deterministic and Windows read-only proof, opens ONE DRAFT PR, posts Issue #16 handoff, releases and stops. No self-review or merge.

A different independent reviewer is required at exact head. CT handles any correction and merge gate separately.

## Deferred live task: office G1

Issue #15 `GRL-012 — Live D/G1 office-laptop portable enrollment` is **GATED** until D source is implemented, independently reviewed and merged. The V26 instruction to run Issue #15 immediately is superseded. Do not substitute manual shell registration for the missing product.

After the D merge, CT must fresh-read authority and exact main, then dispatch the bounded local office-laptop G1 witness. Evidence cannot advance to `WINDOWS_TESTED` before actual live acceptance.

## Standing boundaries

No GitHub-hosted Actions; no Stage-2/cross-repo credentials or workload; no public App distribution; no service/UAC/elevated helper; no arbitrary shell inbox; no personal-laptop enrollment before G1 PASS; no signing/release.

## Next

Dispatch one Checkpoint D source implementation worker on Issue #16. The worker must not execute Issue #15 during source development.
