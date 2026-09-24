# GRL-001 — Independent planning packet

Authority: [Issue #2](https://github.com/bohanyt/github-runner-local/issues/2). Role: ONE independent planner, requested as Opus 5.5 by the owner. Do not presume an external agent was launched.

## Required input

Read main AGENTS/CURRENT, the full selected handoff, Issue #1 and #2 including latest comments, product brief, decisions, platform references, and every file in the rough-design pack selected by CURRENT. Inspect the DRAFT PR diff against its actual base. Fresh-check heads and duplicate/active planner claims before publication.

## Deliverable

Post a self-contained plan on Issue #2; no production code or workflow writes. Include:

1. Read receipt with main/proposal heads and handoff sentinel.
2. Requirement/assumption table: accepted, proposed, corrected, blocked.
3. Recommended architecture and narrowly justified alternatives.
4. Exact identity/permission flows: GitHub login, runner registration, private target checkout, result publication, local user, admin helper and job user.
5. Scope strategy for one runner and multiple personal repositories; do not imply labels overcome GitHub registration scope.
6. Windows setup/service/portable state machines, trusted package download/update/rollback and removal/account-switch behavior.
7. GitHub request/result contract: actual available connector trigger; event actor proof; immutable target; cancellation/replay rules; reporting and offline behavior.
8. Threat model and concrete tests, including malformed inputs, stale HEAD, denied UAC, endpoint policy, low disk, fake result comment, hostile build code and secret isolation.
9. UI screen/state inventory, readable failure messages, accessibility and Documents redirection handling.
10. Phased implementation with dependencies, exact acceptance gates and the smallest first worker packet (allowed files/actions, tests, stop conditions).
11. Decisions requiring owner input, ranked by whether they block the first milestone.
12. Final verdict `PLAN_READY_FOR_REVIEW` or `NEEDS_DECISION`; counted end marker.

## Proof standard

Cite primary platform docs and exact repository paths/refs for claims. Distinguish a proposed prototype from real registration/UAC evidence. Use mocked tests first. Plan an explicitly approved real Windows + real GitHub smoke test later, with a harmless known fixture and no automatic release.

The prototype is a discussion aid. The planner must fix design errors rather than mirror them. Pay particular attention to same-user token access, protected binaries under Documents, Windows runner internal-entrypoint stability and shell restrictions. Profiles constrain requested commands but do not make target code trustworthy.

## Write boundary

Read-only source/design review. A bounded planner claim and one report comment on Issue #2 are permitted; release on Issue #1. Do not merge, create production branches/workflows, register accounts/apps/runners, change machine settings, consume hosted CI or modify other project repositories. New implementation work requires a later reviewed packet.
