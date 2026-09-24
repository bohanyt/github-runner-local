# CURRENT — github-runner-local

Updated: 2026-09-24. Phase: **B_MERGED / C_SOURCE_READY + E_SOURCE_READY_PARALLEL**.

## Authority

- Canonical branch: `main`.
- Control Tower: Issue #1.
- Owner authorization for B merge + C source: Issue #1 comment `5811504614`.
- Owner authorization for parallel E source/template: Issue #1 comment `5811700793`.
- Opus plan: Issue #2 comment `5807784901`.
- CT plan review: Issue #2 comment `5807941253`.
- Current handoff: `docs/control-tower/handoffs/GRL-20260924-C-E-PARALLEL-SOURCE-READY-V11.md`.
- Required sentinel: `END_OF_GRL_HANDOFF key=GRL-20260924-C-E-PARALLEL-SOURCE-READY-V11 sections=9`.

## Completed base

Checkpoint B is merged and complete:
- passing B head `7b7472f910ae54e2de813f90d7d5fa3a60d5f218`;
- merge commit `09c87d00e1741083db000ec21220b34841606310`;
- Issue #8 completed/closed;
- final B evidence remains `LOCAL_CHECKED`.

## Authorized parallel source tasks

### C — local/Windows source task

- Issue #10 — GRL-009
- task mirror: `docs/tasks/GRL-009-c-source.md`
- branch: `feat/grl-c-device-runner-package`
- authority: D25
- requires bounded Windows runner contract probe before final C handoff

### E — cloud-friendly inactive template task

- Issue #11 — GRL-010
- task mirror: `docs/tasks/GRL-010-e-execution-template.md`
- branch: `feat/grl-e-execution-template`
- authority: D26
- full packet: Issue #11 through `END_OF_GRL_E_PACKET key=GRL-010-E-EXECUTION-TEMPLATE-SOURCE-20260924 sections=18`
- CT scope correction: Issue #11 comment `5811742857`
- E implementation branch may change ONLY `templates/execution-repo/**`

C and E may be claimed by different agents concurrently. Their implementation path allowlists are intentionally disjoint.

## E activation remains forbidden

Checkpoint E source is an INACTIVE public template only.

Do NOT:
- create/use private execution repo;
- copy template into a live root `.github/workflows`;
- dispatch any Actions;
- register/start a runner;
- perform connector-trigger live proof;
- implement Stage-2 multi-repo credentials.

Runtime claims such as connector-authored comment delivery and report-only rerun behavior remain activation-time proof.

## Open owner gates

Still OPEN:
- D09 / OD-1: private execution repo;
- D10 / OD-2: GitHub App creation/visibility;
- D21: Stage-2 credential authority;
- D12: service identity/helper;
- D15: license/signing.

D, live E activation, G1, service mode and release remain unauthorized.

## Constraints

No GitHub-hosted Actions. No root workflow mutation. No repository-setting changes. No live runner/App/login activation. No security/power/policy changes. PR #3 remains untouched.

## Next

A cloud GPT agent may claim Issue #11 and implement E source completely on `feat/grl-e-execution-template`, publish one DRAFT PR/handoff, release, and stop. Independently, a local Windows worker may later claim Issue #10 for C. Each task requires independent exact-head review before merge.
