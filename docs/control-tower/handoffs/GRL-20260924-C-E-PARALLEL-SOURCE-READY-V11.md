# github-runner-local — C + E parallel source ready (V11)

## 1. Phase

**B_MERGED / C_SOURCE_READY + E_SOURCE_READY_PARALLEL.** B is complete. Two source-only successors are now authorized in parallel: C (#10) and E (#11). Neither authorizes live activation.

## 2. Authority/read order

Read main `AGENTS.md` → main `docs/CURRENT.md` → this handoff → Issue #1 latest claims → the specific task issue being claimed.

Owner source authorizations:
- C: `5811504614`
- E: `5811700793`

Decisions:
- D25 = C source-only
- D26 = E source/template-only

## 3. Stable base

B exact passing head: `7b7472f910ae54e2de813f90d7d5fa3a60d5f218`.
B merge: `09c87d00e1741083db000ec21220b34841606310`.
Issue #8 is completed.

This continuity transaction adds only authority/task documentation on main.

## 4. Checkpoint C

Issue #10 / GRL-009 remains ready.

Branch: `feat/grl-c-device-runner-package`.

C covers injected/mockable device-flow/REST source, runner pin/download/hash/safe-extraction source, CLI capability contracts, deterministic tests, and a bounded Windows runner contract probe.

Live App/login, private execution repo and runner registration remain forbidden.

## 5. Checkpoint E

Issue #11 / GRL-010 is ready for a separate GPT cloud worker.

Branch: `feat/grl-e-execution-template`.

Full packet ends:
`END_OF_GRL_E_PACKET key=GRL-010-E-EXECUTION-TEMPLATE-SOURCE-20260924 sections=18`.

CT correction `5811742857` tightens implementation writes to:
`templates/execution-repo/**` ONLY.

E covers inactive nested workflow source, local JS actions, Stage-1 same-repo admission/execution/report/verdict logic, fixtures, Node tests and lint.

## 6. Parallel safety

C and E may be worked concurrently by different claimed agents.

Their feature-branch write scopes are disjoint:
- C: Issue #10 allowlist outside `templates/execution-repo/**`
- E: `templates/execution-repo/**` only

No agent may widen its own task to resolve a cross-task concern. Report blockers to its own issue.

## 7. E non-activation boundary

No private execution repo is created or touched.
No root `.github/workflows` is changed.
No workflow is dispatched.
No self-hosted runner is registered/started.
No connector-trigger proof is claimed.
No Stage-2 multi-repo credential/checkout is implemented.

E source tests/lint may run on generic cloud Linux. Evidence ceiling is `LOCAL_CHECKED`.

## 8. Open gates

D09/OD-1 private execution repo: OPEN.
D10/OD-2 GitHub App creation/visibility: OPEN.
D21 Stage-2 credentials: OPEN.
D12 service identity/helper: OPEN.
D15 license/signing: OPEN.

D, live E activation, G1, F and H remain unauthorized.

## 9. Next

A cloud GPT worker may claim E (#11), implement/publish one DRAFT PR, release, and stop. A different agent later performs independent exact-head E review. Local C (#10) may proceed separately when a Windows machine is available.

END_OF_GRL_HANDOFF key=GRL-20260924-C-E-PARALLEL-SOURCE-READY-V11 sections=9
