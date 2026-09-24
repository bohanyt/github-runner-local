# GRL-002 — A1 core contracts

Authority: [Issue #4](https://github.com/bohanyt/github-runner-local/issues/4). This file mirrors the durable issue packet; if they diverge, fresh-read the issue and CT comments and stop for clarification rather than guessing.

## Preconditions

Read main `AGENTS.md`, `docs/CURRENT.md`, the full selected handoff, Issue #1 latest claims, Opus plan #2/`5807784901`, CT review #2/`5807941253`, and Issue #4 in full.

## Scope

Pure deterministic .NET contract/domain layer only: wizard state machine, settings v1, disk admission, explicit host-independent Windows path policy over fakes, request/result/settings schemas and validators.

Windows path semantics tested on Linux must be modeled explicitly; do not rely on Linux `System.IO.Path` behavior as Windows proof.

## Allowed / forbidden

Use the exact allowed/forbidden lists in Issue #4. No workflows, hosted Actions, network, runner/auth/UI/UAC/service/machine/release work.

## Acceptance

Worker-local only:
- `dotnet build -warnaserror`
- `dotnet test`

Report exact test count and limitations. Evidence is `LOCAL_CHECKED`, never `WINDOWS_TESTED`.

## Publication

One branch `feat/grl-a1-core-contracts`, one DRAFT PR, one implementation handoff on Issue #4, then release the Issue #1 claim and stop.

END_OF_GRL_A1_TASK key=GRL-002-A1-CORE-CONTRACTS-20260924 sections=5
