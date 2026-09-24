# Agent instructions

## Read before acting

1. Read `docs/CURRENT.md` from **main**, not an inherited copy on a feature branch.
2. Read the complete handoff selected by CURRENT through its counted end marker.
3. Read Issue #1 and all latest comments, then the active task issue and packet.
4. Read the selected proposal/plan/implementation artifacts only after main authority identifies them.

GitHub is the durable source of project state. Do not rely on another chat, local filesystem persistence, or a remembered HEAD. Reuse evidence already read in a session; fresh-check authority, claims and affected refs before writes. If a required read is truncated, continue to its end.

## Current boundary

The current task is **independent exact-head review of GRL-002 / A1 at PR #5 head `ae2e23a156695f19d9511736a7758b53d4a1406c`**.

Reviewer rules:
- read-only source review;
- no source commits or branch changes;
- no PR edits or merge;
- no CURRENT/DECISIONS edits;
- no GitHub-hosted Actions;
- no runner/auth/App/execution-repo/UI/UAC/service/machine work;
- no later checkpoint implementation.

If PR #5 head moves, stop with `STALE_REVIEW_TARGET`.

## Collaboration

Use `docs/control-tower/PROTOCOL.md`. Reviewer claims bounded review on Issue #1 and releases after posting one review result on Issue #4. Claims are advisory, not atomic locks. Stop on competing ownership or stale authority.

## Evidence

Distinguish `PROPOSED`, `SOURCE_VERIFIED`, `LOCAL_CHECKED`, `WINDOWS_TESTED`, and `OWNER_ACCEPTED`. The implementation worker reported 189/189 local tests; an independent reviewer must verify source/test adequacy and must not upgrade evidence merely by reading the report.

## Handoff

Control Tower updates CURRENT only after the independent review result. Merge remains a separate CT/owner action.
