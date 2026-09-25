# github-runner-local — C review-ready; E needs bounded correction (V17)

## 1. Phase

**C_REVIEW_READY + E_NEEDS_CORRECTION.**

C source remains review-ready. E source was independently reviewed at exact head and returned three bounded blocking findings.

## 2. Authority / read order

Next Control Tower or worker must read:

1. main `AGENTS.md`
2. main `docs/CURRENT.md`
3. this handoff through its sentinel
4. Issue #1 latest claims
5. task issue being acted on

For E correction also read:
- Issue #11 full packet;
- scope correction `5811742857`;
- E-B1 clarification `5812014790`;
- implementation handoff `5824305934`;
- independent review `5824543363` IN FULL;
- correction packet `5824981620` IN FULL.

## 3. Checkpoint C state

Issue #10 / GRL-009.

DRAFT PR #12:
- exact head `67dd220771bf67f65348e622998e005bf30b26bc`;
- branch `feat/grl-c-device-runner-package`;
- open/draft/unmerged;
- implementation handoff `5813138169`;
- implementation release `5813145195`.

Evidence:
- Core 192/192;
- Presentation 45/45;
- Integration 45/45;
- zero skipped;
- Windows read-only runner contract probe PASS.

No independent C review result is currently published.

Latest connector metadata returned mergeable=false. Fresh-check before any later merge action; do not modify C merely to pre-emptively fix mergeability before review.

## 4. Checkpoint E reviewed candidate

Issue #11 / GRL-010.

DRAFT PR #13:
- exact reviewed head `b23d37c4f0367d5311502d2cf80e31453a884d1d`;
- branch `feat/grl-e-execution-template`;
- open/draft/unmerged;
- 33 template-only paths;
- reconciled implementation provenance already preserved;
- implementation handoff `5824305934`;
- release `5824311312`.

Independent review:
- result `5824543363`;
- release `5824545439`;
- disposition `NEEDS_E_CORRECTION`;
- findings `R-E-1`, `R-E-2`, `R-E-3`.

Do not merge this head.

## 5. R-E-1

Location:
`templates/execution-repo/lib/execution.mjs` + `tests/execution.test.mjs`.

Problem:
- JUnit parser can ignore failures/errors in later suites;
- TRX parser can ignore non-passing counters such as timeout/aborted/notRunnable;
- process exit 0 can therefore incorrectly satisfy PASS.

Correction:
- aggregate/validate all supported JUnit suite evidence;
- map or fail closed on supported TRX non-passing counters;
- validate contradictory counts;
- add explicit false-PASS regressions.

## 6. R-E-2

Location:
`templates/execution-repo/lib/admission.mjs` + `tests/admission.test.mjs`.

Problem:
reconciliation duplicate suppression currently trusts a matching public marker/identity without requiring trusted bot authorship and strict note shape.

Correction:
only an exact `github-actions[bot]` authored, strictly validated reconciliation note for the exact request/run/attempt may suppress another note.

Required regressions:
- matching non-bot does not suppress;
- exact matching bot note suppresses;
- malformed/extra-field/wrong-identity notes do not suppress.

## 7. R-E-3

Location:
`templates/execution-repo/tools/lint.mjs` + `tests/lint.test.mjs`.

Problem:
required security linter has meaningful false negatives for:
- normal named-step `run:` blocks;
- extra triggers;
- literal cross-repo checkout;
- Stage-2 credential behavior in local runtime source.

Correction:
harden linter and add mutation-based regressions against real linter inputs.

No new dependency.

## 8. Bounded correction packet

Durable packet:
Issue #11 `5824981620`.

Correction branch/PR:
SAME `feat/grl-e-execution-template` / SAME DRAFT PR #13.

Old head:
`b23d37c4f0367d5311502d2cf80e31453a884d1d`.

Authorized delta: exactly six files:
- `lib/execution.mjs`
- `tests/execution.test.mjs`
- `lib/admission.mjs`
- `tests/admission.test.mjs`
- `tools/lint.mjs`
- `tests/lint.test.mjs`
under `templates/execution-repo/`.

A seventh file requires STOP/CT escalation.

Full local proof must run after correction, including complete Node tests, real linter, schema mirror, PASS/FAIL fixtures, E-B1 zero-process refusal, targeted blocker regressions, `git diff --check`, exact six-file correction delta, and template-only full PR scope.

## 9. Boundaries / parallelism

E correction worker:
- implementation only;
- no self-review;
- no merge;
- no activation;
- no Actions dispatch;
- no private execution repo;
- no Stage-2;
- no C touch.

C review and E correction may proceed in parallel because their source paths/roles are disjoint.

After E correction handoff/release, a DIFFERENT reviewer performs exact-head E correction rereview.

## 10. Next successor action

Preferred immediate next worker:
one local Sol/Codex bounded E correction implementation worker.

It must:
- fresh-check authority/claims/PR #13 head;
- claim `GRL-010-E-RE1-RE3-CORRECTION-20260925`;
- follow Issue #11 packet `5824981620` exactly;
- publish corrected head on SAME PR #13;
- publish one correction handoff;
- release;
- stop.

Next Control Tower after that:
- record corrected exact head;
- issue independent correction-rereview packet if not already explicit;
- preserve C review state;
- merge nothing without PASS.

END_OF_GRL_HANDOFF key=GRL-20260925-C-REVIEW-READY-E-NEEDS-CORRECTION-V17 sections=10
