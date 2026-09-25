# github-runner-local — C + E both review-ready (V16)

## 1. Phase

**C_REVIEW_READY + E_REVIEW_READY.**

Both source checkpoints are fully implemented, published as DRAFT PRs, and released by their implementation workers.

## 2. Authority/read order

Read main `AGENTS.md` → main `docs/CURRENT.md` → this handoff → Issue #1 latest claims → relevant task issue.

C task: Issue #10 / GRL-009.
E task: Issue #11 / GRL-010.

## 3. C candidate

PR #12:
- exact head `67dd220771bf67f65348e622998e005bf30b26bc`
- 13 changed paths
- handoff `5813138169`
- release `5813145195`
- evidence: Core 192, Presentation 45, Integration 45, zero skipped, runner probe PASS

Independent review only. No merge until PASS.

## 4. E candidate

PR #13:
- exact head `b23d37c4f0367d5311502d2cf80e31453a884d1d`
- 33 template-only changed paths
- handoff `5824305934`
- release `5824311312`
- 98 tests / 0 skipped
- linter/schema/PASS/FAIL/E-B1 proof PASS

Independent review only. No merge until PASS.

## 5. E reconciliation provenance

Remote stale cloud:
`e13f868be0c42f0327c530a03e2e4ac884e9da75`

Original local:
`dbba8b3e25d38fc6a03d8981bc6bcbd3a52c940c`

Selected integrated source:
`724b580de2d2795ee1a0f35695cfe73896894942`

Published merge:
`b23d37c4f0367d5311502d2cf80e31453a884d1d`

Publication preserved both histories and advanced the remote branch normally without force. Final tree equals selected integrated source tree.

## 6. C review focus

Review device-flow, token redaction, private/admin gating, runner pin/hash/download/extraction safety, CLI capability/version constraints, deterministic tests, and Windows contract-probe safety/evidence.

## 7. E review focus

Review workflow prefilter/security, parser/replay/expiry/branch containment, profile Git blob SHA, argv execution, exact-SHA refusal semantics, report/verdict completeness, schema mirror discipline, linter strength, action pins, and reconciliation provenance.

## 8. Boundaries

No C/E merge in reviewer role.
No activation, Actions dispatch, private execution repo, runner registration, Stage-2 credentials, D/G1/service/release work.

## 9. Next

One independent reviewer may review C and a separate independent reviewer may review E in parallel. Each publishes one task-issue verdict, releases its claim, and stops.

END_OF_GRL_HANDOFF key=GRL-20260925-C-E-BOTH-REVIEW-READY-V16 sections=9
