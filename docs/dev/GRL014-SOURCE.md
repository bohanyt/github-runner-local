# GRL-014 revision 2 SOURCE candidate

This package implements Issue #18 packet `5853140232` against design PR #23
`ccd580aec36432c4b40eeedecf3a04fab17f9a97`. It is disabled in normal
`LiveWizardComposition`. No existing installation is migrated. `JobGateSession`
requires explicit `JobGateCapability.Synthetic`; the offline ownership transports
require both the synthetic capability and an offline fake request provider.
There is no live transport, control repository, enrollment or deployment here.

## Local adapter and hook contract

`GatedRunnerProcessAdapter` verifies exact embedded hook bytes and checkpointed
configuration, writes startup INACTIVE, then delegates start. Missing/foreign hooks,
configuration drift, unreadable state and interrupted locks refuse the operation;
there is no ungated fallback. `JobGateSession` exposes local activation, BeginRelease,
CloseAdmission, planned Pause and warned emergency Stop Now. Activation/Resume uses
a fresh gate epoch and requires the exact sole-registration snapshot plus a complete
no-owned-Worker observation. This relies on **no concurrent enrollment**; the snapshot
is not distributed exclusion. Planned closure can refuse later downstream work;
liveness waiting is a caller decision, never authority to terminate work.

The hook contract is pinned to actions/runner
`397b032cbf865e9c3ddfab89d533ec19325e1273` (v2.337.0):

- [HostContext.cs](https://github.com/actions/runner/blob/397b032cbf865e9c3ddfab89d533ec19325e1273/src/Runner.Common/HostContext.cs):
  `GetDefaultShellForScript` selects bundled Node for `.js`.
- [JobHookProvider.cs](https://github.com/actions/runner/blob/397b032cbf865e9c3ddfab89d533ec19325e1273/src/Runner.Worker/JobHookProvider.cs):
  the hook is a ScriptHandler step; a missing script fails.
- [ScriptHandler.cs](https://github.com/actions/runner/blob/397b032cbf865e9c3ddfab89d533ec19325e1273/src/Runner.Worker/Handlers/ScriptHandler.cs)
  exports runtime contexts. [GitHubContext.cs](https://github.com/actions/runner/blob/397b032cbf865e9c3ddfab89d533ec19325e1273/src/Runner.Worker/GitHubContext.cs)
  includes repository, run, attempt, job, SHA and workflow SHA;
  [RunnerContext.cs](https://github.com/actions/runner/blob/397b032cbf865e9c3ddfab89d533ec19325e1273/src/Runner.Worker/RunnerContext.cs)
  exports `RUNNER_NAME`. Numeric identity is the checkpointed `GRL_GATE_RUNNER_ID` pointer configuration.
- [JobExtension.cs](https://github.com/actions/runner/blob/397b032cbf865e9c3ddfab89d533ec19325e1273/src/Runner.Worker/JobExtension.cs):
  started hook precedes job steps; completed hook is an always post-job step.
  Its per-job `_processLookupId` GUID becomes inherited `RUNNER_TRACKING_ID`
  before step execution. This, plus repo/runner/run/attempt/job/SHA, identifies
  the invocation without relying on a reusable PID or workspace file.
- [Listener Program.cs](https://github.com/actions/runner/blob/397b032cbf865e9c3ddfab89d533ec19325e1273/src/Runner.Listener/Program.cs)
  loads runner `.env`. Actual Worker inheritance remains **A1 pending**.

JS support is a pinned-source fact, not a promise for every runner version.
[Public hook documentation](https://docs.github.com/en/actions/how-tos/manage-runners/self-hosted-runners/run-scripts)
describes Bash/PowerShell, `.env` pointers and runner restart requirements. Hook scripts,
state, ledger and configuration checkpoint are in a separate ordinary product-owned
directory outside both the extracted application directory and `_work`. Reparse
ancestors/descendants, drive roots and nested placement are refused. The source adapter
does not bypass execution or organization policy.

## Ordering, identity and recovery

One cross-process atomic-directory lock serializes **every** hook decision and state
transition. Intent is written and file-flushed before state is read; ledger is flushed
before PASS. Records are correlated with a SHA-256 invocation identifier. Decisions
and completion survive duplicates; old epochs/attempts and changed identities cannot
reuse PASS or ledger provenance. Downstream checks validate the referenced admit
intent and PASS as well as the current local epoch/attempt/workflow SHA.

State replacement uses a flushed same-volume temporary file and rename. Partial job
records are uncertainty. An abandoned lock is **never automatically stolen or deleted**;
startup/stop stays unavailable pending an eventual authorized recovery procedure.
Records are retained. Exact no-Worker evidence may record a `GATE_ANOMALY` for an
unresolved intent or crashed passed job. It never edits portable recovery JSON to
manufacture Paused. Startup from a pass-capable state and emergency/failed termination
record recovery-required, blocking reactivation. Power-loss/directory-metadata durability
is not claimed beyond the tested Windows filesystem ordering (A4).

`.done` is **not process-exit evidence**. Planned Pause requires INACTIVE, valid records,
and two complete exact owned-lifetime observations, including one immediately before
stop. Windows Toolhelp enumeration, creation time and process image bind ownership;
previously observed work lifetimes are retained across disappearance/reparenting.
Worker images from the same runner directory block even after their parent exited.
Listener/batch/console infrastructure may remain alive until the planned stop. Other
owned descendants conservatively block. Permission errors, partial snapshots, root
lifetime changes and unknown exit observations block. PID reuse distinguishes the
old lifetime. The caller binds observation to the process it actually owns; the tests
use disposable owned children, never an adopted office process.

Configuration installation checkpoints the original bytes/existence before replacement,
preserves comments/unrelated entries, refuses foreign pointers, supports interrupted
retry/idempotence, and restores exact bytes. Reinstallation after restoration archives
the old checkpoint first. Drift is preserved and refused. Checkpoint bytes are private
local configuration, never log/handoff material. Only synthetic fixtures are installed
in this source campaign; no real runner `.env` or credential file is read or changed.

## Ownership and workflow inventory

Ownership candidates require one exact observed parent, valid monotonic epochs and the
exact holder identity. The offline REST-shaped adapter emits empty-tree commits,
create-if-absent bootstrap and `force:false` ref updates. It verifies returned commit
identity and rejects merge parents. Losing acquires are not rebased/retried as a new
operation. Durable publication intent precedes commit creation; pending RELEASED
blocks Abort/reactivation across restart even when reads still show self-owned ACQUIRED.
Only exact operation/head reconciliation resolves it; unresolved/losing operations
remain quarantined. Release requires a current gate-epoch drain acknowledgement from
the lifetime-proof caller. No remote takeover is implemented.

Lint inventories step conditions and the exact pinned action metadata. The only allowed
unconditional step is the existing exact upload-artifact block in execute. Unknown
always/failure/cancelled exceptions, unsupported condition forms, pre actions and
unreviewed post behavior fail closed. Checkout's reviewed post cleanup is retained;
it is registered when its main step ran. Metadata snapshots and normalized SHA-256
digests are committed for offline reproduction. Workflow/action runtime bytes are
unchanged; no private deployment occurs.

A refused job may still execute the permitted artifact upload with **stale grl-outcome
files present**. No claim of empty workspace or zero post-refusal activity is made.
Those files are not gate decisions or new admission provenance. Gate permission does
not replace the workflow's actual admission, actor, profile and reporting checks.
Attempt-specific provenance deliberately refuses downstream-only reruns without a new
local admit. Full reruns can obtain their own new-attempt admission. Refused requests
may need reposting (OD-C pending).

## Reproduce safely

Use the SDK selected by `global.json` and existing Node (Node 24.14.1 was used):

```powershell
dotnet build src/Grl.App/Grl.App.csproj -warnaserror
dotnet test tests/Grl.Integration.Tests/Grl.Integration.Tests.csproj
dotnet test tests/Grl.App.Presentation.Tests/Grl.App.Presentation.Tests.csproj
dotnet test tests/Grl.Core.Tests/Grl.Core.Tests.csproj
node --test tests/job-gate/gate.test.cjs
node --test "templates/execution-repo/tests/*.test.mjs"
node templates/execution-repo/tools/lint.mjs
git diff --check
```

These tests create disposable roots, actual Node hook children, Windows synthetic
Worker-shaped children and fake HTTP/ref transports. They never register a runner,
invoke the runner's configuration CLI, run a live job, launch the product UI or use a
live ownership ref. `GRL_TEST_NODE` can select an existing explicit Node path for .NET
tests without changing global PATH. Build output stays in the separate source worktree.

D1-T covers both competing deliveries, all 20 bounded send/deliver/activate interleavings,
duplicates, exact-parent/holder/epoch errors, delayed release/lost response/restart/Abort
and fake HTTP shapes. D2-T calls the filesystem implementation through all 90 two-job
start/completion/closure/proof schedules, three concurrent actual hooks, intent barriers,
completion-child barriers and a real process crash. D3-T covers wrong identity/repository,
SHA/epoch/attempt refusal, full/downstream-only reruns, historical workflows, stale
workspace PASS and harmless profile markers. Negative controls retain the rejected
label/quiet/read-first/done-only counterexamples; their results are not designer counts.

Evidence is LOCAL_CHECKED, including Windows synthetic checks. A1 real pinned Worker
environment inheritance, A5 live Git-ref concurrency semantics, OD-G, all live witnesses
and the later independent integrated source/design review remain pending. This document
does not claim WINDOWS_TESTED, OWNER_ACCEPTED, merge permission or live readiness.
