# CURRENT — github-runner-local

Updated: 2026-09-25. Phase: **C_MERGED + E_MERGED + OD1_OD2_APPROVED + LIVE_BOOTSTRAP_WAITING_OWNER_UI**.

## Authority

- Canonical branch: `main`.
- Control Tower: Issue #1.
- Current handoff: `docs/control-tower/handoffs/GRL-20260925-OD1-OD2-APPROVED-LIVE-BOOTSTRAP-V25.md`.
- Required sentinel: `END_OF_GRL_HANDOFF key=GRL-20260925-OD1-OD2-APPROVED-LIVE-BOOTSTRAP-V25 sections=8`.

## Merged source checkpoints

Checkpoint C:
- passing head `c062957407acf8f7d70c46767345e85c737f39f4`
- independent PASS `5826166918`
- merge commit `451d3ab889f9f63f45ebf90ba54ddece7b60e6eb`

Checkpoint E:
- passing head `e2fb5a3152998990ea12e92be4aef921d1d19387`
- independent PASS `5825645126`
- merge commit `a296f3f9dfa362f6a53d20ff81a46eef7a519819`

Post-merge continuity before OD approvals:
`2c1c90b061c2fada6055f71bab6575809315018c`.

## Owner-approved live gates

D09 / OD-1: **ACCEPTED**.

Dedicated private execution repository for first live acceptance:
`bohanyt/github-runner-local-exec`.

Do not reuse an existing project repository.

D10 / OD-2: **ACCEPTED**.

Acceptance GitHub App:
- owner-only/private acceptance use;
- Device Flow enabled;
- not public for general distribution yet;
- runtime Client ID is allowed;
- no client secret/private key in product source or execution repo.

Durable packet:
Issue #14 — `GRL-011 — Live acceptance bootstrap: owner-only GitHub App + private execution repo`.

## Manual prerequisite state

The connected GitHub toolset can mutate existing repositories but does not expose:
- creation of a new repository;
- registration/creation of a GitHub App.

Therefore the owner must create these account-level objects in GitHub UI:

1. private repo exactly `bohanyt/github-runner-local-exec`;
2. owner-only GitHub App with Device Flow enabled.

Proposed App display name:
`github-runner-local-dev`.

After creation, provide the App Client ID (not a secret) or state that both objects exist.

## Still-open gates

D21 / Stage-2 multi-repo checkout credential authority: **OPEN**.

Also still gated:
- D12 service identity/helper;
- D15 license/signing;
- public GitHub App distribution;
- service/UAC mode;
- release work.

First live acceptance remains same-execution-repo only.

## No-hosted-Actions constraint

D19 remains ACCEPTED:
**do not use GitHub-hosted Actions**.

First live acceptance must execute only on the reviewed self-hosted Windows runner path.

## Live bootstrap packet

Issue #14 defines the bounded next path:
- verify private execution repo;
- verify owner-only Device-Flow App;
- materialize reviewed execution template;
- perform owner Device Flow;
- select only the private execution repo;
- register one portable non-admin runner;
- run only harmless same-repo fixture acceptance;
- collect evidence and stop.

No Stage-2 cross-repo credentials or workload checkout are allowed.

## Next

Owner completes the two GitHub UI prerequisites from Issue #14.

Then Primary CT fresh-verifies available repository/App evidence and publishes the bounded D/G1 live-execution packet.

No further C/E source merge is pending.
