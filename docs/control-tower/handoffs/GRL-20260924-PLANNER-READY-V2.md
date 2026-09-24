# github-runner-local — planner-ready handoff V2

## 1. Scope and phase

The owner selected the exact name `github-runner-local` and requested immediate repository setup, durable agent continuity and rough design. That bootstrap is published. Phase: `FOUNDATION_READY / AWAITING_INDEPENDENT_PLANNER`. This is not a finished installer or permission to deploy a runner.

## 2. Authority

Read `AGENTS.md` and `docs/CURRENT.md` from **main**, then this full handoff through its end marker, Issue #1 including latest comments, Issue #2 including latest comments, and `docs/tasks/GRL-001-planning.md`. The root CURRENT.md is only a navigation pointer. A design branch can contain an inherited stale CURRENT; do not use it instead of main.

Issue #1 is Control Tower/claims/owner direction. Issue #2 is the independent planner task. Both are durable project records, not a live runner command inbox. V1 is preserved as historical bootstrap context; V2 supersedes it.

## 3. Published refs

- Repository: public `bohanyt/github-runner-local`, already created but empty before this session.
- Initial README: `c4b527d157855b84d6c0fd447d3910b5a4e80779`.
- Foundation / proposal merge base: `e551e63e7fc703ef11254c5813c643c51ea0a5eb`.
- Rough design: SAME **DRAFT PR #3**, branch `design/grl-001-foundation-v0`.
- Candidate head: `0f009a1fefefeec45c40ced8f172a0373746fe12`.
- Candidate tree: `5ffb3fbd2c0af40eeeaebea36b98ba2cc32b811c`.

The final Issue #1 publication comment pins the main commit containing this handoff. Do not attempt to put a commit's own eventual hash inside that commit. Fresh-check refs/claims before any new write.

## 4. Accepted owner requirements

Public reusable Windows GUI wizard; browser/device GitHub sign-in; easy changes of laptop/account; one runner first; prefer one ordinary folder in Documents on C:; normal UAC consent when a bounded setup operation needs admin; GitHub request/result loop instead of copying terminal output. Keep GitHub as project authority. The next external pass is an owner-relayed **Opus 5.5 planner** in Claude chat, not a coding worker.

Earlier three-runner and terminal-first directions are superseded. Exact CPU/RAM and actual available tools are not verified; constrained disk is a design consideration, not a measured machine report. No guaranteed speed claims.

## 5. Hard boundaries

No production implementation, hosted Actions dispatch/rerun, self-hosted dispatch, machine enrollment, actual credentials, OAuth app registration, service install, release, paid service or change to another repository under this task. No policy/EDR/UAC bypass, arbitrary comment-to-shell command, public fork execution or automatic source push-back. The public product repository must not become an unrestricted execution inbox.

Do not use Windows No Sleep as a reason to change sleep/lock policy. Its previous `asInvoker`/`runas` pattern is inspiration only, not a certification of reusable helper IPC or proof that this runner works.

## 6. Material to read

Main: product brief, decision ledger, primary references R1-R11, security guidance, Control Tower role/protocol and planning packet.

Candidate PR #3: `design/README.md`, `ARCHITECTURE.md`, `WIZARD.md`, `THREAT_MODEL.md`, `REQUEST_RESULT_PROTOCOL.md`, `ACCEPTANCE.md`, `BOOTSTRAP_VALIDATION.md`, both example JSON files, `wizard-preview.html`, and the three `tools/` files. Read the entire diff/pack. The HTML is a seven-panel offline simulation, not the chosen production UI framework. Example fixture reports are invented and marked example-only.

## 7. Open architecture decisions

One repo-level registration cannot serve all personal repositories by matching labels. Proposed path: one-private-repo pilot, then a separately approved private execution hub if needed. Organization scope is an alternative for organization-owned repos; no repo migration is authorized. A hub requires explicit private checkout/report permissions and its own result/check mapping.

Choose actual auth adapter and permissions, native UI/runtime, verified runner package/integration surface, credential lifetime/store, updater/rollback, safe cleanup, license and signing. None is settled by a mock screen. A GUI integration with internal runner executables needs a compatibility proof, not an assumed public API.

## 8. Security and UX conflicts to resolve

Keep normal UI/login and job execution non-admin. Protected helper/service binaries and ancestor directories must not be replaceable by job code. A service may need protected locations outside Documents; obtain owner approval for that exception or defer service mode. No silent LocalSystem shortcut.

A folder is not a sandbox; private visibility and fixed profiles do not make target code safe. Same-user encrypted credentials are not isolated from that user's jobs. Define a meaningful job/setup credential boundary or explicitly limit the mode to trusted code with disclosed risks.

Native GUI plus UAC success does not mean cmd, PowerShell or build subprocesses will run under endpoint policy. Preflight must exercise the real required tools and stop honestly when blocked. Show redirected/OneDrive Documents before choosing a local path. Account sign-out, runner unregistration and local cleanup are separate actions.

## 9. Request and return design

Candidate dispatch is an authenticated issue comment in a private execution repo, handled by its reviewed default-branch workflow. Prove the connector's actual sender and event delivery; do not assume arbitrary workflow_dispatch is exposed or a token-generated comment triggers another workflow.

Use exact target SHA, trusted profile revision, immutable request/run/attempt identities, actor authorization, expiry, replay protection, cancellation and bounded data parsing. GitHub event/default-branch SHA is not automatically the target SHA. Reporting must distinguish test outcome from upload outcome, and a copied PASS comment is not proof.

Normal Actions logs/status differ from custom summaries/comments/artifacts. Default GITHUB_TOKEN is repo-scoped. No source auto-push. An ordinary idle chat is not automatically resumed by a GitHub result; active/later agents reread it, and notifications require separately supported configuration.

## 10. Evidence obtained and missing

Temporary Linux checks passed: `python tools/check_design.py`; 26 `unittest` checker tests; Playwright Chromium 144.0.7559.96 on seven panels at 1280x1000 and 390x844 with zero page errors/HTTP(S) requests during checked interactions. One desktop screenshot was visually inspected. All 13 candidate Git blobs match local tested source.

Public git transport failed DNS; connector Git operations worked. Browser `file://` navigation was blocked by the harness, so tests used `set_content` without policy changes. No native Windows/file-launch/auth/UAC/service/runner/live-job acceptance, isolated-execution proof or release exists. See `docs/BOOTSTRAP_REPORT.md` and candidate `design/BOOTSTRAP_VALIDATION.md`.

## 11. Next bounded planner task

Owner relays a prompt to Opus 5.5. Planner reads complete authority/candidate, checks duplicate claims, then posts a bounded claim on Issue #1 and one full plan on Issue #2. Report must contain read receipt/refs, corrected assumptions, recommended architecture, concrete auth/privilege/scope/protocol flows, threat tests, UI states, phased gates and the smallest first implementation-worker packet.

Allowed writes: planning claim, complete report comment, release. No source implementation, workflow activation, PR merge, registration, machine change or release. Stop at `PLAN_READY_FOR_REVIEW` or `NEEDS_DECISION`. A later reviewed/owner-approved packet assigns production work; no such worker is active now.

## 12. Continuity and claims

Bootstrap claim is Issue #1 comment `5807377508`; final publication/release on that same issue closes it. Check latest comments rather than assuming this session still owns the scope. No external planner or background process was launched. The next CT continues the entire product through planning, implementation and acceptance under explicit stage authority, not merely maintaining this mock.

END_OF_GRL_HANDOFF key=GRL-20260924-PLANNER-READY-V2 sections=12
