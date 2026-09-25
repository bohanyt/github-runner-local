# github-runner-local — C review-ready; E correction review-ready (V18)

## 1. Phase

**C_REVIEW_READY + E_CORRECTION_REVIEW_READY.**

Checkpoint C remains independently review-ready. Checkpoint E correction for R-E-1/R-E-2/R-E-3 is published and awaits a DIFFERENT independent exact-head correction rereviewer.

## 2. Authority / read order

Next Control Tower or reviewer must read:

1. main `AGENTS.md`
2. main `docs/CURRENT.md`
3. this handoff through its sentinel
4. main `docs/DECISIONS.md`
5. `docs/control-tower/ROLE.md`
6. `docs/control-tower/PROTOCOL.md`
7. Issue #1 latest claims/releases
8. the task issue being acted on

For E correction rereview additionally read in full:
- independent E review `5824543363`;
- correction packet `5824981620`;
- correction handoff `5825291040`;
- independent rereview packet `5825299636`.

## 3. Checkpoint C state

Issue #10 / GRL-009.

DRAFT PR #12:
- branch `feat/grl-c-device-runner-package`;
- exact candidate head `67dd220771bf67f65348e622998e005bf30b26bc`;
- open / draft / unmerged;
- implementation handoff `5813138169`;
- worker release `5813145195`.

Evidence remains:
- Core 192/192;
- Presentation 45/45;
- Integration 45/45;
- zero skipped;
- Windows RunnerContractProbe PASS;
- runner v2.337.0 exact pin/hash verified.

No independent C review result has been published. Fresh connector check after E correction reports PR #12 mergeable=true. This is only metadata; do not merge before independent PASS.

## 4. Checkpoint E prior review

Issue #11 / GRL-010.

Old reviewed head:
`b23d37c4f0367d5311502d2cf80e31453a884d1d`.

Independent review:
- claim `5824425643`;
- result `5824543363`;
- release `5824545439`;
- disposition `NEEDS_E_CORRECTION`;
- findings `R-E-1`, `R-E-2`, `R-E-3`.

CT correction packet:
`5824981620`.

## 5. E corrected candidate

SAME branch / SAME DRAFT PR #13:
- branch `feat/grl-e-execution-template`;
- corrected exact head `855bd3342376ca7695a7d19d119d578986d09c2f`;
- open / draft / unmerged;
- current connector mergeable=true;
- full PR remains exactly 33 paths, all under `templates/execution-repo/**`.

Correction worker:
- claim `5825042626`;
- correction handoff `5825291040`;
- release `5825294298`.

Old→new correction delta is exactly six files:
- `templates/execution-repo/lib/execution.mjs`
- `templates/execution-repo/tests/execution.test.mjs`
- `templates/execution-repo/lib/admission.mjs`
- `templates/execution-repo/tests/admission.test.mjs`
- `templates/execution-repo/tools/lint.mjs`
- `templates/execution-repo/tests/lint.test.mjs`

## 6. E correction substance

R-E-1:
- JUnit aggregates all suites and validates aggregate contradictions;
- TRX validates supported counters and fails closed on timeout/aborted/notRunnable and other ambiguous non-passing states;
- false-PASS regressions added.

R-E-2:
- reconciliation dedupe now trusts only exact `github-actions[bot]` strict-shape notes for exact request/run/attempt;
- human/malformed/extra-field/wrong-identity notes do not suppress durable reconciliation.

R-E-3:
- linter rejects normal named-step `run:`, extra workflow triggers, literal/expression checkout `repository:` overrides, and Stage-2 credential constructs in executable runtime source;
- real-input mutation regressions added.

## 7. E corrected-head proof

Correction handoff `5825291040` records exact-source worker-local proof:

- Node v22.16.0;
- complete Node campaign: 106 passed / 0 failed / 0 skipped;
- real template linter PASS;
- schema mirrors byte-for-byte PASS;
- PASS fixture: 1 process / PASS;
- FAIL fixture: 1 process / expected FAIL;
- E-B1 refusal: 0 profile processes / BLOCKED;
- targeted R-E-1/R-E-2/R-E-3 regressions PASS;
- `git diff --check` PASS;
- six-file correction delta verified;
- 33-path template-only full PR scope verified.

Direct clone DNS was unavailable in the temporary compute environment; files were materialized through the GitHub connector and relevant blob identities were checked against the corrected GitHub head. No hosted Actions were dispatched.

## 8. E independent rereview dispatch

Durable rereview packet:
Issue #11 comment `5825299636`.

The next E reviewer must be DIFFERENT from the correction worker and Primary CT.

Exact target:
`855bd3342376ca7695a7d19d119d578986d09c2f`.

Reviewer must fresh-claim on Issue #1, independently verify all three blocker closures and full E invariants, publish one exact-head disposition on Issue #11, release, and stop.

No E merge before independent PASS.

## 9. Boundaries / gates

Still forbidden unless separately owner-authorized:
- private execution repo creation/use;
- live template activation;
- GitHub Actions dispatch/rerun;
- runner registration/start;
- Stage-2 credentials;
- GitHub App creation/live login;
- Checkpoint D / G1;
- service/UAC work;
- release/signing.

C and E reviews may proceed independently in parallel because they are read-only roles over disjoint candidate lineages.

## 10. Next successor action

Primary next action:
- one DIFFERENT independent reviewer takes the E correction rereview packet `5825299636` and reviews exact head `855bd3342376ca7695a7d19d119d578986d09c2f`.

In parallel:
- one independent C reviewer may review exact C head `67dd220771bf67f65348e622998e005bf30b26bc`.

After either review result, Control Tower fresh-checks claims/refs, records the exact disposition, and updates continuity. Merge nothing without independent PASS.

END_OF_GRL_HANDOFF key=GRL-20260925-C-REVIEW-READY-E-CORRECTION-REVIEW-READY-V18 sections=10
