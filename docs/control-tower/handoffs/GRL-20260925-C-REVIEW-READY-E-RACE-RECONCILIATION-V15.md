# github-runner-local — C review-ready; E race reconciliation ready (V15)

## 1. Phase

**C_IMPLEMENTED / AWAITING_INDEPENDENT_EXACT_HEAD_REVIEW + E_RACE_RECONCILIATION_READY.**

C remains review-ready. E has two competing implementation lineages that must be reconciled without force-push.

## 2. Authority/read order

Read main `AGENTS.md` → main `docs/CURRENT.md` → this handoff → Issue #1 latest comments → task issue.

E reconciliation authority:
Issue #11 comment `5823097901`.

## 3. C state

DRAFT PR #12 exact head:
`67dd220771bf67f65348e622998e005bf30b26bc`.

C is independent-review-only until PASS.

## 4. E competing candidates

Remote stale cloud head:
`e13f868be0c42f0327c530a03e2e4ac884e9da75`.

- one commit after old E base;
- 31 template files;
- pushed after cloud claim revocation;
- no PR/handoff;
- not automatically accepted.

Unpublished local head:
`dbba8b3e25d38fc6a03d8981bc6bcbd3a52c940c`.

Reported local proof:
87 tests / 0 skipped, linter PASS, schema mirror PASS, PASS/FAIL fixture PASS, E-B1 zero-process refusal PASS, 33 template files.

## 5. Required reconciliation proof

One local reconciliation worker must run full proof on BOTH exact candidates and compare source against:
- full Issue #11 packet;
- scope correction `5811742857`;
- exact-SHA clarification `5812014790`.

Do not choose based on origin/author alone.

## 6. Selection and no-force publication

Select the smallest stronger compliant tree or integrate compliant differences.

If remote tree wins, it may be published after proof.

If local/integrated tree wins:
- preserve remote cloud history;
- preserve local selected history;
- create reconciliation merge with first parent remote E head, second parent selected local commit, tree exactly selected local tree;
- push canonical E branch fast-forward only;
- no force push.

Final diff against current main must remain only:
`templates/execution-repo/**`.

## 7. Publication

Create exactly one DRAFT PR to current main after full proof.

Publish the standard 9-section E implementation handoff, including comparative proof and reconciliation provenance.

Then release and stop.

Independent E review follows.

## 8. Boundaries

No activation, Actions dispatch, private execution repo, runner activation, Stage-2 credentials, C source/merge, D/G1/service/release, or self-review.

## 9. Next

Run bounded local E reconciliation/publish. C independent review can proceed separately.

END_OF_GRL_HANDOFF key=GRL-20260925-C-REVIEW-READY-E-RACE-RECONCILIATION-V15 sections=9
