# CURRENT — github-runner-local

Updated: 2026-09-24. Phase: **B_MERGED / C_SOURCE_READY**.

## Authority

- Canonical branch: `main`.
- Control Tower: Issue #1.
- Owner authorization for B merge + C source: Issue #1 comment `5811504614`.
- Opus plan: Issue #2 comment `5807784901`.
- CT plan review: Issue #2 comment `5807941253`.
- Active task: Issue #10 — GRL-009 Checkpoint C source: device flow, runner pin/package, CLI contract.
- Task mirror: `docs/tasks/GRL-009-c-source.md`.
- Current handoff: `docs/control-tower/handoffs/GRL-20260924-B-MERGED-C-SOURCE-READY-V10.md`.
- Required sentinel: `END_OF_GRL_HANDOFF key=GRL-20260924-B-MERGED-C-SOURCE-READY-V10 sections=8`.

## Checkpoint B completion

- Original B implementation: Issue #8 comment `5810381568`.
- First independent review: `5811022199` → `NEEDS_B_CORRECTION`, R-B-1/R-B-2.
- Correction: `5811298851`.
- Independent correction rereview: `5811461145` → `PASS_B_CORRECTION_EXACT_HEAD`, findings=0.
- Exact reviewed B head: `7b7472f910ae54e2de813f90d7d5fa3a60d5f218`.
- PR #9 merged by merge commit `09c87d00e1741083db000ec21220b34841606310`; parents are prior main `5d7b7af8e4dce2509fc50d5150cb8339b87ef067` and reviewed B head.
- Merge diff from prior main is exactly the 19 B paths.
- Issue #8 closeout comment: `5811549859`; Issue #8 is completed/closed.
- Final B evidence remains `LOCAL_CHECKED`: Core 192/192, Presentation 45/45, classifier self-test 7/7, UI smoke S1–S5 PASS.

## Checkpoint C authority

D25 authorizes source-only C under Issue #10.

C may implement:
- injected/mockable GitHub App device-flow and REST adapter;
- official runner pin/download/SHA-256/safe staged extraction;
- runner CLI version/capability contracts;
- deterministic tests and a bounded Windows runner contract probe.

C must NOT perform live App/login/runner activation.

## Open gates

Still OPEN:
- D09 / OD-1: create dedicated private execution repo;
- D10 / OD-2: create GitHub App and choose acceptance-stage visibility;
- D21: stage-2 multi-repo credential authority;
- D12: service identity/elevated helper;
- D15: license/signing.

Checkpoint D, execution-template activation, G1, service mode and release remain unauthorized.

## Constraints

No GitHub-hosted Actions. No repository-setting changes. No runner registration/start. No live device flow. No private execution repo. No service/UAC/policy/security/power changes. PR #3 remains untouched.

## Next

One bounded local implementation worker may claim GRL-009, branch from current main on `feat/grl-c-device-runner-package`, execute Issue #10 through its end marker, publish one DRAFT PR and implementation handoff, release the claim, and stop. Independent exact-head review follows.
