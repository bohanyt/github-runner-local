# Coordination protocol

## Authority and reads

Main `docs/CURRENT.md` selects the active handoff and task. Owner decisions in Issue #1 prevail over proposals. Conflicts are reported, not silently resolved. A branch's inherited CURRENT may be stale; always read main first. After an initial complete read, reuse evidence and refresh only state needed for authority, HEAD, claims, duplicate detection or writes.

## Claims

Before a bounded write, read the latest Issue #1 comments and affected refs. Post a claim containing actor/role, task, branch/path scope, expected base, permitted actions, expiry and stop conditions. Recheck for competing claims. Claims are advisory: GitHub issue comments are not an atomic lock. On conflict, stop. Release on completion or publish a blocked handoff. Do not automatically steal an expired claim without checking evidence of active work.

## Publication

Use one task branch and one DRAFT PR. Record exact base/head, changed files, commands, results and missing proof. No force pushes or unrelated edits. A successful source write is not a successful test. During planning, comments/design documents are allowed; executing or enabling workflows is not.

## Result and handoff

Publish summaries beside the task authority. Separate `PASS`, `FAIL`, `BLOCKED`, `CANCELLED`, `TIMED_OUT` and `REPORTING_INCOMPLETE`; do not hide failed checks in success counts. A handoff uses numbered `##` sections and a final `END_OF_GRL_HANDOFF key=... sections=N` line. CURRENT points to exactly one successor. Immutable commit IDs in comments identify publication; avoid a self-referential HEAD inside its own commit.

## Cost and scope

No hosted Actions dispatch or rerun, including release/test automation, until the owner changes that rule. Local temporary-compute validation is allowed when safe and disclosed. No laptop connection, setup, credentials or policy changes are available merely because an agent can write GitHub.
