# CURRENT — github-runner-local

Updated: 2026-09-24. Phase: **A1_READY / AWAITING_IMPLEMENTATION_WORKER**.

## Read this from main

This is the sole current-state pointer. Read `AGENTS.md`, this file, the full selected handoff, Issue #1 latest claims, Issue #2 plan/review, and the active task before acting.

## Authority and active task

- Canonical branch: `main`.
- Control Tower: [Issue #1](https://github.com/bohanyt/github-runner-local/issues/1).
- Opus plan: [Issue #2 comment 5807784901](https://github.com/bohanyt/github-runner-local/issues/2#issuecomment-5807784901).
- CT review/corrections: [Issue #2 comment 5807941253](https://github.com/bohanyt/github-runner-local/issues/2#issuecomment-5807941253), disposition **`ACCEPTED_FOR_A1_WITH_CORRECTIONS`**.
- Active task: [GRL-002 / Issue #4 — A1 core contracts](https://github.com/bohanyt/github-runner-local/issues/4).
- Task packet mirror: [GRL-002 A1](tasks/GRL-002-a1-core-contracts.md).
- Current handoff: [GRL-20260924-A1-READY-V3](control-tower/handoffs/GRL-20260924-A1-READY-V3.md).
- Required end marker: `END_OF_GRL_HANDOFF key=GRL-20260924-A1-READY-V3 sections=10`.

## Plan status

Opus plan `5807784901` is **accepted only far enough to begin A1**. CT corrections `CT-C1..CT-C8` remain authoritative. In particular: Windows path logic must be host-independent; no GitHub-hosted Actions; GitHub App/execution repo/office-laptop/service/multi-repo work is not authorized by A1.

The earlier rough-design DRAFT PR #3 remains open/unmerged at candidate head `0f009a1fefefeec45c40ced8f172a0373746fe12`. It is a planning artifact, not the A1 implementation branch.

## A1 boundaries

A1 is pure core/contracts:
- `.NET 10 + xUnit`;
- deterministic state machine/settings/disk logic;
- explicit Windows path-policy model behind fakes;
- request/result/settings schemas and strict validators;
- worker-local `dotnet build -warnaserror` and `dotnet test` only.

Forbidden in A1: workflows, hosted Actions, network adapters, process launching, registry, real runner packages, auth/token storage, UI/WPF, UAC/service changes, execution-repo/App creation, release or merge.

## Open owner gates for later checkpoints

No owner decision is required for A1. Before live registration: OD-1 private execution repo, OD-2 GitHub App, OD-7 office-laptop enrollment. Service mode remains deferred; license/signing remain release gates. Hosted Actions remain disabled until explicitly re-enabled.

## Next bounded action

One implementation worker may claim GRL-002 on Issue #1, implement **only** Issue #4 on branch `feat/grl-a1-core-contracts`, publish one DRAFT PR + exact local test evidence, release the claim, and stop. No implementation worker has been launched by this CT review.

