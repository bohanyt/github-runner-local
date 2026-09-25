# CURRENT — github-runner-local

Updated: 2026-09-25. Phase: **OFFICE_D_G1_READY**.

## Authority

- Canonical branch: `main`.
- Control Tower: Issue #1.
- Current handoff: `docs/control-tower/handoffs/GRL-20260925-OFFICE-D-G1-READY-V26.md`.
- Required sentinel: `END_OF_GRL_HANDOFF key=GRL-20260925-OFFICE-D-G1-READY-V26 sections=8`.

## Merged source checkpoints

Checkpoint C and E are merged and independently review-clean.

C merge:
`451d3ab889f9f63f45ebf90ba54ddece7b60e6eb`.

E merge:
`a296f3f9dfa362f6a53d20ff81a46eef7a519819`.

## Live prerequisites

OD-1 / D09: ACCEPTED.
- private execution repo: `bohanyt/github-runner-local-exec`
- independently verified private / default branch `main`

OD-2 / D10: ACCEPTED.
- owner-only acceptance GitHub App
- Device Flow enabled
- not public yet
- owner supplied runtime Client ID in Issue #14
- installation setup is owner-attested because connector cannot inspect private App settings

OD-7 / D29: ACCEPTED.
- current OFFICE WINDOWS LAPTOP authorized for first live portable enrollment / G1
- runner name: `grl-office`
- portable, interactive-user, non-admin
- trusted-code-only
- same execution repo fixture only

## Conditional second runner

D30: ACCEPTED, but only after G1 PASS.

Future personal runner:
`grl-personal`.

Both may remain registered to the same private execution repo, but while they share `grl-exec` the operating rule is:

**ONE ACTIVE AT A TIME.**

Do not enroll the personal laptop before G1 PASS.

## Live D/G1 packet

Issue #15:
`GRL-012 — Live D/G1 office-laptop portable enrollment`.

That packet authorizes the bounded live steps:
- bootstrap reviewed execution template into private repo;
- create/configure mailbox issue + repository variables;
- real owner Device Flow;
- select exact private execution repo;
- download/verify/install reviewed runner;
- register `grl-office`;
- start portable runner unelevated;
- run harmless same-repo fixture acceptance;
- run bounded safe negative witnesses;
- publish evidence and stop.

## Still forbidden

- Stage-2 / cross-repo checkout or credential minting
- GitHub-hosted Actions
- service mode / elevated helper / UAC path
- public GitHub App distribution
- arbitrary shell inbox
- external/unreviewed project code
- personal-laptop enrollment before G1 PASS
- release/signing work

## Evidence target

G1 is not complete until actual live evidence exists.

Source/local proofs remain green but do not substitute for:
- real Device Flow witness
- runner online witness
- self-hosted workflow run
- request/ACK/result/verdict round trip
- safe negative witnesses

## Next

Run one bounded LOCAL worker on the CURRENT OFFICE LAPTOP using Issue #15.

Worker must claim, execute only the authorized D/G1 packet, publish exact evidence, release, and stop.

No self-declared PASS without the actual live witness.
