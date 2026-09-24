# Agent instructions

## Read before acting

1. Read `docs/CURRENT.md` from **main**, not an inherited copy on a feature branch.
2. Read the complete handoff selected by CURRENT through its counted end marker.
3. Read Issue #1 and all latest comments, then the active task issue and packet.
4. Read the selected proposal/plan artifacts only after main authority identifies them. A DRAFT PR is not an approved implementation plan.

GitHub is the durable source of project state. Do not rely on another chat, local filesystem persistence, or a remembered HEAD. Reuse evidence already read in a session; fresh-check authority, claims and affected refs before writes. If a required read is truncated, continue to its end.

## Working boundaries

- Current implementation authorization is **GRL-002 / A1 core contracts only**, as selected by main `docs/CURRENT.md`.
- No work outside the exact A1 allowed paths/APIs. In particular: no UI/WPF shell yet, no auth adapter, no workflow activation, no runner download/registration, no GitHub App or execution-repo creation, no UAC/service/machine change, no merge/release.
- **Do not use GitHub-hosted Actions** until the owner explicitly re-enables them. A1 proof is worker-local compute only.
- Public installer source is separate from any later private/authorized execution target. Never register the office runner to this public development repo.
- No tokens, credentials, private project output, machine identifiers, or actual workplace information in commits, comments, screenshots, or artifacts.
- Do not disable security policy, exclusions, UAC, antivirus, AppLocker or App Control. No elevated execution of arbitrary CI jobs.
- Windows path-policy logic tested on Linux must be explicit host-independent Windows semantics; Linux path behavior is not Windows acceptance.

## Collaboration

Use `docs/control-tower/PROTOCOL.md`. Claim bounded writes on Issue #1 and release at completion. Claims are advisory, not atomic locks. Stop on competing ownership, stale authority, or unclear authorization. Keep one task branch/DRAFT PR rather than creating duplicates. No force-push, merge, auto-merge or release unless specifically assigned.

For GRL-002, use exactly one branch `feat/grl-a1-core-contracts` and one DRAFT PR. The worker must not edit Control Tower/current/decision files; publish evidence on Issue #4 and release the claim.

## Evidence

Distinguish `PROPOSED`, `SOURCE_VERIFIED`, `LOCAL_CHECKED`, `WINDOWS_TESTED`, and `OWNER_ACCEPTED`. Never upgrade one into another. Publish exact refs, commands, outcomes and limitations. A mock preview is not a working installer; Linux contract tests are not Windows filesystem acceptance; a result comment is not proof without matching run/attempt/target identity.

## Handoff

Update the main CURRENT pointer only when authorized for coordination. A successor handoff must state decisions, open questions, issue/PR/branch refs, evidence, next bounded task, and an explicit end marker. Product requirements belong in the brief/decision ledger, not hidden inside a chat.
