# github-runner-local — revision 2 Sol source-only implementation (V51)

## 1. Phase and authority

**GRL014_REV2_SOURCE_IMPLEMENTATION_READY; LIVE_ACTIVATION_GATED; GRL015_ACCEPTED.**

Active issue: #18. Active source packet: comment `5853140232`, read in full through:
`END_OF_GRL014_SOURCE_PACKET key=GRL014-REV2-SOL-SOURCE-20260927 sections=6`.

CT disposition: `GRL014_REV2_ACCEPTED_AS_SOURCE_BASELINE_WITH_CONSTRAINTS`. This authorizes an inactive source candidate and its tests, not independent design-review PASS, merge, or live activation.

The owner selected a NEW local Sol session as the next implementation worker. The previous Opus design session may remain parked; another general design round is not required first.

## 2. Design identity and retained proof

Design-only DRAFT PR #23 remains unmerged on `design/grl-014-safe-switching`, exact reference head `ccd580aec36432c4b40eeedecf3a04fab17f9a97`.

Read `docs/design/GRL014_SAFE_SWITCHING_PROTOCOL.md` at that SHA. Its revision-2 §0 controls over SUPERSEDED revision-1 sections. Handoff: Issue #18 `5853104447`. Designer claim `5852965996` released by `5853105794`.

Revision 2 proposes a runner-local hook gate and serialized ownership ref rather than label/variable fencing. Designer model counts are reported design evidence, not reproduced product tests. A1 hook environment inheritance, A4 local ordering and A5 ref semantics require implementation proof and later witnesses as appropriate.

Historical G1 and GRL-015 stay accepted. Office identity is replacement `grl-office` ID 3, last witnessed version 2.337.0. ID 2 is removed/historical. The accepted running product was built from `1e74ffdbe9d64dc96bb30007c72af80c625b6946`; private execution main was `2c8da8834e785ba012001b9427bd68380401bec7`. No current local state was observed or changed by this CT turn.

## 3. Source phase and implementation constraints

One NEW implementation branch (preferred `feat/grl014-job-gate`) from fresh main and ONE new DRAFT source PR. Do not mutate/merge PR #23 or reset the old checkout. Preserve existing worktrees, notes, runner roots and active build outputs.

Required S1/S2/S5/S6: gate hooks/state/ledger/intent/configuration, opt-in local lifecycle and proof-gated stop, narrow template lint and reproducible tests. S3/S4 ownership client and cross-machine transitions may be implemented against fakes, with the live capability disabled until OD-G.

The source packet adds mandatory engineering constraints:
- `.done` is not proof that the admitted Worker has exited; JOB_COMPLETED is a post step before job completion. Do not kill a live admitted Worker based on its marker.
- Hook/state files live outside `_work` and outside the extracted runner application directory; `.env` pointers stay in the runner directory. Preserve existing `.env` and foreign hooks.
- Test actual intent/decision/completion correlation and Windows synchronization, not only an idealized model. Process-query failure is uncertainty, not absence.
- Pending/delayed RELEASED publication prevents local abort/reactivation until reconciled. Fast-forward-only updates require exact-parent transition validation; they are not a general unconstrained CAS.
- Refusal must preserve profile safety and reporting truth with stale artifacts, job pre/post steps, reruns and old workflow SHAs. Unknown inventory fails closed.
- Gate-enabled code must not fall back to ungated execution. Normal live composition is not automatically migrated or enabled by this source work.

Useful primary references checked by CT:
- https://docs.github.com/en/actions/how-tos/manage-runners/self-hosted-runners/run-scripts
- https://docs.github.com/en/rest/git/refs
- actions/runner `397b032cbf865e9c3ddfab89d533ec19325e1273`: `src/Runner.Common/HostContext.cs`, `src/Runner.Worker/JobHookProvider.cs`, `src/Runner.Worker/JobExtension.cs`.

The pinned source supports `.js` via bundled Node; current public hook documentation lists Bash/PowerShell. Do not conflate pinned implementation behavior with a universal documented guarantee.

## 4. Evidence and publication

One broad implementation claim covers workspace reconciliation, implementation, targeted development tests, final complete affected suites, normal push, DRAFT PR, Issue #18 exact-head handoff and release. Use existing tooling after discovery; no global auth/PATH/policy reset or automatic installer work.

Commit reproducible tests of the actual implementation, including D1-T/D2-T/D3-T and negative controls. One final affected-suite campaign is sufficient absent a concrete code change/failure. Do not repeat G1, GRL-015 or historical full campaigns for document changes.

The local goal ends at **SOURCE_IMPLEMENTATION_REVIEW_READY** plus evidence and claim release. It does not wait for a future review or live switching. An independent integrated source/design review and separate CT merge/live decision follow later.

## 5. Owner and live boundaries

Only source implementation is authorized now. No installation of hooks into the real runner, live `.env` changes, live runner job, Stop Now, restart/reboot, label/variable changes, App permission change, private execution/control-repo write, personal enrollment, service/autostart or hosted Actions.

OD-G remains pending; do not create a control repo. OD-A/C/D and live OD-F remain pending. OD-B is withdrawn. Remote release/forced takeover and scheduled keep-alive remain unavailable. Missing OD-G does not prevent fake-tested source implementation.

ARCHIVE/CHECKPOINT FIRST. Preserve office ID 3 and all accepted evidence. No self-review/merge or force-push. No further Opus task or live activity is started by this handoff.

END_OF_GRL_HANDOFF key=GRL-20260927-REV2-SOL-SOURCE-READY-V51 sections=5
