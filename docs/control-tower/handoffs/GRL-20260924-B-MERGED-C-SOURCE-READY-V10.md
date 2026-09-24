# github-runner-local — B merged, C source ready (V10)

## 1. Phase

**B_MERGED / C_SOURCE_READY.** Checkpoint B is independently review-clean and merged. Checkpoint C source implementation is authorized but live GitHub/runner activation is still gated.

## 2. Authority

Read main `AGENTS.md` → main `docs/CURRENT.md` → this handoff → Issue #1 latest comments → Issue #10 full packet. Owner authorization is Issue #1 comment `5811504614`; D25 records the bounded C source authority.

## 3. B completion

B exact passing head: `7b7472f910ae54e2de813f90d7d5fa3a60d5f218`.
PASS rereview: Issue #8 comment `5811461145`.
Merge commit: `09c87d00e1741083db000ec21220b34841606310`, merge method `merge`.
Parents: prior main `5d7b7af8e4dce2509fc50d5150cb8339b87ef067` and reviewed B head.
Issue #8 closeout: `5811549859`.
Final B evidence: Core 192/192; Presentation 45/45; classifier 7/7; S1–S5 PASS; `LOCAL_CHECKED`.

## 4. Active C task

Issue #10 / GRL-009 is active. Task mirror: `docs/tasks/GRL-009-c-source.md`.
Branch must be `feat/grl-c-device-runner-package` from the current main after this continuity transaction.
One DRAFT PR only.

The full Issue #10 packet defines:
- device-flow/REST adapter with injected HTTP and memory-only token semantics;
- expected official runner pin `2.337.0` / SHA-256 `1150692afa94e71f872017e254ea55b6eece1eece3fe7e3a6d4c93d0a1b85cfc`, subject to fresh official verification;
- safe staged download/extraction;
- `IRunnerCli` / version capability contract;
- deterministic tests and Windows contract probe;
- exact allowed files and stop conditions.

## 5. Live gates remain closed

D09/OD-1 private execution repo remains OPEN.
D10/OD-2 GitHub App creation/visibility remains OPEN.
No live device-flow login, no token persistence, no runner registration/start, no private execution repo, no D/E activation, no hosted Actions.

If the official runner pin has drifted, the worker stops with `RUNNER_PIN_DRIFT`; it does not silently repin.

## 6. C evidence target

Worker-local, non-elevated Windows:
- solution build/test green;
- Core stays 192/192 and Presentation 45/45, zero skipped;
- Integration tests report exact count, zero skipped;
- Windows contract probe verifies exact official runner asset/hash/version and required CLI help/capabilities without registering/running a runner;
- exact branch diff stays inside Issue #10 allowlist.

Evidence ceiling: `LOCAL_CHECKED`; official runner metadata can be `SOURCE_VERIFIED` when fresh official evidence is recorded.

## 7. Constraints

No product App/Presentation/Core changes unless Issue #10 explicitly allows them (it does not). No workflows/templates activation, no PR #3 changes, no package expansion, no hosted Actions, no service/UAC/policy/security/power/release work, no Checkpoint D.

## 8. Next

One local Sol worker may claim GRL-009, implement/publish C on the exact branch, release, and stop. A different independent reviewer then performs exact-head review before any merge or live activation decision.

END_OF_GRL_HANDOFF key=GRL-20260924-B-MERGED-C-SOURCE-READY-V10 sections=8
