# github-runner-local — C review-ready; E transferred to local (V14)

## 1. Phase

**C_IMPLEMENTED / AWAITING_INDEPENDENT_EXACT_HEAD_REVIEW + E_LOCAL_TAKEOVER_READY.**

Checkpoint C is review-ready. Checkpoint E has been transferred from a lagging cloud ChatGPT worker to one local Codex worker.

## 2. Authority/read order

Read main `AGENTS.md` → main `docs/CURRENT.md` → this handoff → Issue #1 latest comments → task issue.

C: Issue #10 / GRL-009.
E: Issue #11 / GRL-010.

Owner E transfer authority:
Issue #1 `5813834606`.

Issue #11 transfer publication:
`5813835204`.

## 3. C exact candidate

DRAFT PR #12 exact head:
`67dd220771bf67f65348e622998e005bf30b26bc`.

C remains independent-review-only until PASS. E takeover does not alter C.

## 4. E cancelled cloud claim

Cloud E claim:
`5812970520`.

It is explicitly cancelled/revoked by the owner.

At cancellation:
- E branch existed;
- branch head = `cd6f6abd13de9af2122d1c61db774b2dc54f7da0`;
- branch diff from base = zero;
- no E PR;
- no E handoff.

Any later writes/publication from that cancelled cloud worker are stale and must not be accepted without CT reconciliation.

## 5. E local takeover

Successor local worker reuses:
`feat/grl-e-execution-template`.

Do not create a second E branch.

Fresh-read Issue #11 full packet and:
- scope correction `5811742857`;
- blocker `5811944203`;
- protocol clarification `5812014790`.

Then post a NEW implementation claim before source writes.

## 6. E write/proof scope

Absolute E write scope:
`templates/execution-repo/**` ONLY.

Local proof:
- node --test;
- template linter;
- PASS/FAIL fixture behavior;
- exact-SHA refusal behavior;
- schema mirror check;
- git diff --check;
- exact changed-path check.

No Windows-specific proof is required.

## 7. E-B1 exact-SHA refusal

Requested A / checkout B mismatch:
- zero profile processes;
- no canonical result;
- no fabricated tested_sha;
- separate `grl-exec-refusal v1`;
- failing commit status on requested A;
- verdict fails;
- complete refusal reporting => BLOCKED;
- incomplete refusal reporting => REPORTING_INCOMPLETE.

## 8. Activation boundaries

No private execution repo.
No root workflow activation.
No Actions dispatch.
No runner activation.
No Stage-2 credentials.
No D/G1/service/release work.

## 9. Next

One local Codex worker claims and completes E on the existing empty branch, publishes one DRAFT PR + 9-section Issue #11 handoff, releases, and stops. A different reviewer later reviews E.

END_OF_GRL_HANDOFF key=GRL-20260924-C-REVIEW-READY-E-LOCAL-TAKEOVER-V14 sections=9
