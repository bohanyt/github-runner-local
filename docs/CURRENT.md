# CURRENT — github-runner-local

Updated: 2026-09-24. Phase: **FOUNDATION_READY / AWAITING_INDEPENDENT_PLANNER**.

## Read this from main

This is the sole current-state pointer. An inherited copy on a design branch may be stale. Read `AGENTS.md`, this file, the full selected handoff, and the latest task/coordination comments before acting.

## Authority and active task

- Canonical branch: `main`.
- Control Tower: [Issue #1](https://github.com/bohanyt/github-runner-local/issues/1).
- Active task: [GRL-001 / Issue #2](https://github.com/bohanyt/github-runner-local/issues/2), **independent planning only**.
- Packet: [GRL-001 planning](tasks/GRL-001-planning.md).
- Current handoff: [GRL-20260924-PLANNER-READY-V2](control-tower/handoffs/GRL-20260924-PLANNER-READY-V2.md).
- Required end marker: `END_OF_GRL_HANDOFF key=GRL-20260924-PLANNER-READY-V2 sections=12`.
- Historical bootstrap V1 is superseded by this V2 pointer.

## Selected rough-design candidate

- **DRAFT PR #3:** [wizard design and execution-bridge planning pack](https://github.com/bohanyt/github-runner-local/pull/3).
- Branch: `design/grl-001-foundation-v0`.
- Candidate head: `0f009a1fefefeec45c40ced8f172a0373746fe12`.
- Candidate tree: `5ffb3fbd2c0af40eeeaebea36b98ba2cc32b811c`.
- Foundation / merge base: `e551e63e7fc703ef11254c5813c643c51ea0a5eb`.
- Read [design/README.md at the candidate](https://github.com/bohanyt/github-runner-local/blob/0f009a1fefefeec45c40ced8f172a0373746fe12/design/README.md), all linked pack files, and `design/BOOTSTRAP_VALIDATION.md`.

There are 13 proposal files: architecture, wizard states, threat model, protocol, acceptance, preview, invented fixtures, validation record and three local checking tools. The proposal is not an approved implementation plan. Do not merge it merely to start planning.

## Evidence and limits

Local design checks passed; 26 checker regression tests passed; seven-panel offline preview passed Chromium checks at 1280x1000 and 390x844. All 13 remote candidate blobs match the locally staged/tested files. See [bootstrap report](BOOTSTRAP_REPORT.md).

**No installable application, Windows acceptance, actual login, UAC/service operation, runner registration, live job or release exists.** No hosted Actions or self-hosted jobs were run; no workflow YAML is installed. Public distribution is not a public execution mailbox.

## Next bounded action

The owner will relay one prompt to Opus 5.5 in Claude chat. The planner reads main authority plus the selected candidate, checks latest claims, publishes one complete source-grounded plan on Issue #2, and stops at `PLAN_READY_FOR_REVIEW` or `NEEDS_DECISION`.

No implementation worker or external planner has been launched. Bootstrap claim `5807377508` is closed by its final publication/release comment on Issue #1; verify latest comments before a new claim. Future implementation requires plan review and a separately authorized bounded packet.
