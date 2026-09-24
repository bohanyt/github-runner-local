# Agent instructions

## Read before acting

1. Read `docs/CURRENT.md` from **main**, not an inherited copy on a feature branch.
2. Read the complete handoff selected by CURRENT through its counted end marker.
3. Read Issue #1 and all latest comments, then the active task issue named by CURRENT.
4. Read proposal/plan/implementation artifacts only after main authority identifies them.

GitHub is the durable source of project state. Do not rely on another chat, local filesystem persistence, or a remembered HEAD. Reuse evidence already read in a session; fresh-check authority, claims and affected refs before writes. If a required read is truncated, continue to its end.

## Current boundary

The active task, exact targets, lease and write boundaries are defined ONLY by main `docs/CURRENT.md` and the task issue it selects. This file intentionally contains no task-specific heads. If this file and CURRENT disagree, CURRENT wins; report the discrepancy.

## Standing rules

No GitHub-hosted Actions until the owner re-enables them (D19). No runner/auth/App/execution-repo/UAC/service/machine-policy work unless the active task explicitly authorizes it. Implementers never review their own work. Merge is a separate CT/owner action.

## Collaboration

Use `docs/control-tower/PROTOCOL.md`. Claims and releases belong on Issue #1. Claims are advisory, not atomic locks. Stop on competing ownership or stale authority.

## Evidence

Distinguish `PROPOSED`, `SOURCE_VERIFIED`, `LOCAL_CHECKED`, `WINDOWS_TESTED`, and `OWNER_ACCEPTED`. Do not upgrade evidence by reading a report. `WINDOWS_TESTED` is reserved for real-integration acceptance (D24).

## Handoff

Control Tower updates CURRENT after results. Published comments and docs never contain local absolute paths, usernames, machine names or credentials.
