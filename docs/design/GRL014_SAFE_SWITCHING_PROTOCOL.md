# GRL-014 — Safe admission fence and one-active-at-a-time switching protocol

Status: **PROPOSED design / evidence plan** for Issue #18. Design only. Nothing in this
document was executed against a live runner, workflow, variable or label.

Authority read for this design: public main `02c1e167d0d232d4b76a1bd6ba1beb87c17be9bd`
(CURRENT → V50 handoff through `END_OF_GRL_HANDOFF key=GRL-20260927-GRL015-ACCEPTED-GRL014-DESIGN-V50 sections=5`),
Issue #18 body and comments `5828869070` and `5852242889`, GRL-015 acceptance Issue #21 `5852242726`,
advisory audit Issue #1 `5828798876`. Private execution main `2c8da8834e785ba012001b9427bd68380401bec7`
was inspected read-only.

Statement labels used throughout:

- **FACT** — verified in source at an exact SHA/tag or quoted from current official documentation (appendix §22).
- **INFERENCE** — reasoned from facts; not documented as a guarantee; needs a witness where noted.
- **PROPOSED** — design choice made by this document; not accepted.
- **OWNER DECISION REQUIRED** — changes an owner-approved operating model or permission.

Project evidence level of this whole document: `PROPOSED`, with individual source facts `SOURCE_VERIFIED`.
No `LOCAL_CHECKED` or `WINDOWS_TESTED` claim is made.

---

## 1. Executive summary

**Recommendation (PROPOSED): a two-layer fence with drain in between.**

1. **Layer 1 — request admission gate.** A new repository variable `GRL_ADMISSION` is added to the
   server-side job-level `if:` of the `admit` job only. Admission is open only when the value is exactly
   `open`; missing or any other value means closed. When closed, a new request's `admit` job is skipped by
   GitHub **before any runner assignment**, and the other three jobs are skipped by their existing
   conditions. No self-hosted job is created.
2. **Drain to terminal.** Wait until the repository has **zero non-terminal workflow runs**. This covers
   all four jobs of every already-accepted request and the idle gaps between them.
3. **Layer 2 — exact-ID runner routing fence.** Remove the custom label `grl-exec` from the exact
   numeric runner (repo + ID + name re-verified). Every GRL job requires `grl-exec`. After this, no GRL job
   can be *newly* assigned to that runner, while the listener keeps running and nothing is interrupted.
4. **Frozen-set verification.** After the label fence, the set of jobs that can ever run on the old runner
   is frozen: only jobs assigned before the fence. Re-verify, over a settle window, that there are zero
   non-terminal runs and the runner is not busy. If anything appears, **roll the fence back** by re-adding
   the label. Never stop in that case.
5. **Deactivate.** Only then stop the product-owned process. Stop Now is a tree kill.
6. **Activate the other machine in reverse order.** Start its existing registration *without* `grl-exec`,
   prove it is online and idle, add `grl-exec` to that exact ID, then set `GRL_ADMISSION=open`.

**Why it is safe:** the layers use independent server mechanisms: a repository variable and runner labels.
For the switch to cancel a job, three things must all fail: a new job must be created despite closed
admission, it must be assigned despite the label fence, and it must escape two verification reads
separated by the settle window. Every failure of a single layer is detected and handled by waiting or
rolling back, never by stopping. `busy=false` is never used as a drain signal on its own. It is only read
*after* the fence has frozen the set of assignable work.

**Two persistent registrations survive.** `grl-office` (ID 3) and a future `grl-personal` both stay
registered. "Active" means "holds `grl-exec`". No live DELETE, no re-registration and no credential
movement are needed for a switch. See §14 for the limits: the 14-day auto-removal and the D30 wording.

**Owner decisions required (§20):** re-interpret D30's "both share `grl-exec`" as "`grl-exec` is held only
by the active runner"; grant (or decline) Actions-variables permission to the GitHub App; accept skipped
requests while admission is closed; a 14-day keep-alive policy; and authorization of implementation plus
live witnesses.

The rejected alternatives are exact-ID DELETE as the primary fence, `busy=false`+Stop Now,
variable-routed `runs-on`, workflow disable, concurrency groups, runner groups and ephemeral runners (§8).

---

## 2. Current proven system

### 2.1 Office runner (FACT, per CT-accepted WINDOWS_TESTED operator evidence)

- Runner `grl-office`, numeric **ID 3**, version **2.337.0**, labels `self-hosted, Windows, X64, grl-exec`,
  registered to private repo `bohanyt/github-runner-local-exec`. Historical ID 2 is removed and is not a
  target.
- Portable mode: the product starts `run.cmd` in an owned root as the unelevated user. The runner lives
  while the product window is open.
- Proven operations: register, online, js-smoke PASS, planned Stop Now, same-root same-ID resume, and
  resume after reboot (Issue #21 `5845412219`, `5852213379`).

### 2.2 Product lifecycle (FACT, public main `02c1e16`)

- `PortableRunnerLifecycle` states: `Ready, Configuring, Configured, Starting, Online, Draining, Paused,
  Degraded, RemoteRemovalPending, Removed` (`src/Grl.Integration/PortableRunnerLifecycle.cs` L3–7).
- `DrainAsync` always fails with `DrainUnavailable` and changes nothing (L177–186; D31).
- `StopNowAsync` requires a live owned process and records `Paused` with the warning that an active job
  may have been cancelled (L188–198).
- The underlying stop is `Process.Kill(entireProcessTree: true)` (`PortableRunnerProcess.cs` L313–317).
  This is **harsher than the runner's own Ctrl+C path**: the worker is killed without cancellation
  reporting.
- `UnregisterAsync` refuses while an owned process exists or a surviving process is possible. It requires
  the exact ID to be offline and not busy, then runs `config.cmd remove` (L324–395).
- Identity: the numeric ID is resolved after configure and persisted. `RestorePlannedPause` and resume
  require exactly one runner with the persisted ID and name, offline and not busy (L276–309).
- `GitHubRunnerAdministration` (`src/Grl.Integration/GitHubRunnerAdministration.cs`) provides:
  - `ListAsync`, which parses `id, name, status, busy` but **not labels**;
  - registration and remove tokens;
  - `DeleteStaleAsync`, which re-lists and refuses unless the exact ID/name is **offline and not busy**.

  There is no label, variable or workflow-run API.
- The recovery store persists the non-secret `runnerId`, state and `MayHaveUnownedProcess`
  (`PortableRecoveryStore.cs`).

### 2.3 Private execution workflow (FACT, private main `2c8da88`)

The private tree is **blob-identical** to `templates/execution-repo/` on public main. All 34 files were
compared by blob SHA. `.github/workflows/grl-dispatch.yml`:

| Job | Server-side condition (evaluated before assignment) | runs-on |
|---|---|---|
| `admit` | mailbox issue == `vars.GRL_MAILBOX_ISSUE` ∧ not PR ∧ login ∈ `vars.GRL_AUTHORIZED_ACTORS` ∧ `author_association == 'OWNER'` ∧ body starts with `<!-- grl-request v1 -->` | `[self-hosted, Windows, X64, grl-exec]` |
| `execute` | `needs: admit`; `needs.admit.outputs.admitted == 'true'` | same |
| `report` | `needs: [admit, execute]`; `always() && needs.admit.outputs.admitted == 'true'` | same |
| `verdict` | `needs: [admit, execute, report]`; `always() && needs.admit.result != 'skipped'` | same |

- Trigger: `issue_comment: created`. There is no `concurrency:` key, so overlapping requests run in
  parallel runs. With one runner, their jobs interleave.
- Admission (`lib/admission.mjs` `admitRequest`) runs **on the runner**. It re-checks the prefilter,
  envelope, expiry, profile digest, the request edit/withdraw state, SHA containment, replay/duplicate,
  triggering actor, disk, elevation and capabilities. It then posts one ACK comment.
- Machine-specific admission facts (disk, elevation, capability) are measured **on the machine that runs
  `admit`**. `execute` does not re-measure them. **INFERENCE:** if `execute` ran on a different laptop from
  `admit`, it would run on an unadmitted machine. This is the request-affinity property I3 (§3).
- `reconcileRecent` posts `grl.reconcile.v1` notes for terminal-but-incomplete runs.
- Lint (`tools/lint.mjs`) pins the admit `if:` clauses (L20–28) and the exact `runs-on` string for every
  job (L34).

### 2.4 What is authoritative at each phase (FACT/INFERENCE)

| Phase | Authoritative state |
|---|---|
| Request exists | The issue comment. It is re-read by `admit`. |
| Admission decision | The ACK comment `grl.ack.v1` (admitted/reason) posted by `admit`. |
| Work outstanding | Workflow run status and job status on GitHub. A run is non-terminal while any job can still be created or run. |
| Execution result | The result comment, the commit status `grl/<profile>` and the `verdict` job result. |
| Runner identity | `GET /repos/{repo}/actions/runners` entry: exact numeric ID + exact name. The product also holds the persisted ID. |
| Runner eligibility | The runner's label set on GitHub (server-side routing) and its online status. |
| Local process | Product-owned process handle. After an app restart, only persisted evidence remains (`MayHaveUnownedProcess`). |

---

## 3. Safety property / invariant

PROPOSED invariants for any switching protocol:

- **I1 No interruption.** The switch never terminates a runner process while any job is assigned to it.
  This covers the whole span from server assignment to job completion.
- **I2 Single active.** At every instant, at most one registration in the execution repo holds `grl-exec`.
  A switch opens admission only when exactly one holder exists and it is online.

  A holder may later go offline while admission stays open, for example the existing planned
  pause/reboot. That state (`ACTIVE_DEGRADED`) is safe: requests queue on the server, up to 24 h, until
  that holder resumes.
- **I3 Request affinity.** All self-hosted jobs of an admitted request run on the runner that ran its
  `admit` job. Violations must be prevented by ordering and detected (and rolled back) if they appear.
- **I4 Truthful uncertainty.** Any unreadable, ambiguous or contradictory observation yields a pending or
  blocked state. Nothing is ever automatically stopped, deleted, relabelled-to-active or reopened on
  uncertainty.
- **I5 Exact identity.** Every mutation targets repo full name + numeric runner ID + exact name, re-verified
  by a fresh read immediately before the mutation. There is no name-only adoption and no historical ID 2.

"Active" is defined as **holding `grl-exec`**. Being online alone is not active.

---

## 4. Threat and race model

Actors: the owner (UI actions, request comments, re-runs), agents posting requests, the GitHub Actions
service (run creation, `if` evaluation, job assignment, API caches), the two portable listeners, and the
product on each laptop. The product may crash or close at any instruction. The network may fail or time
out with an uncertain mutation result. The API may be rate limited or stale.

Races that matter:

| ID | Race |
|---|---|
| R-a | New request vs admission close |
| R-b | Job queued vs runner fence (assignment decided just before the fence, delivered after) |
| R-c | Idle gap between `admit` → `execute` → `report` → `verdict` of one run |
| R-d | Overlapping runs interleaving jobs on one runner |
| R-e | Owner re-run of an old run (a new attempt creates jobs without a new comment) |
| R-f | Stale list/runs responses (ARC notes list responses cached around 60 s — FACT for ARC's observation, INFERENCE for today) |
| R-g | Variable propagation delay; resolution timing of `vars` is undocumented (§6.3) |
| R-h | Listener death or app crash mid-switch |
| R-i | Second laptop failure after the first is fenced |

---

## 5. Current-source facts (actions/runner v2.337.0)

Tag `v2.337.0` = commit `397b032cbf865e9c3ddfab89d533ec19325e1273` (verified by clone).

- **F5.1 Ctrl+C / unload.** `CtrlCHandler` → `HostContext.ShutdownRunner(UserCancelled)`
  (`src/Runner.Listener/Runner.cs` L359–391). The message loop exits.
- **F5.2 Shutdown cancels running jobs.** On exiting the loop, `jobDispatcher.ShutdownAsync()`
  (Runner.cs L852–858) calls `EnsureDispatchFinished(currentDispatch, cancelRunningJob: true)`, which
  cancels the worker (`JobDispatcher.cs` L206–250). There is no local "stop accepting jobs, finish current"
  mode. `run` accepts only `--once` (deprecated in favour of `--ephemeral`), `--jitconfig` and
  `--startuptype`.
- **F5.3 Registration disappears while listening.** A `TaskAgentNotFoundException` or
  `RunnerNotFoundException` from `GetNextMessageAsync` is caught in the loop (Runner.cs L823–830): "The
  runner no longer exists on the server. Cleaning up local configuration." The loop breaks and the
  `finally` runs `ShutdownAsync` (cancel) and then `DeleteLocalRunnerConfig()` (L877–880). `RunAsync`
  returns `Success` (0).
- **F5.4 Other deletion-shaped errors take other paths.** `VssUnauthorizedException`,
  `AccessDeniedException` and `TaskAgentSessionExpired` (when recovery fails) are non-retriable. In the V2
  broker listener they are wrapped in `NonRetryableException` (`BrokerMessageListener.cs` ~L335–350,
  `IsGetNextMessageExceptionRetriable` ~L445–460). `Program.cs` maps:
  - `NonRetryableException` → `TerminatedError` (1);
  - `RunnerNotFoundException` → 1;
  - any other exception → `RetryableError` (2) (Program.cs ~L130–160).

  **Local config is deleted only on the F5.3 path.** Which exception a deleted registration actually
  produces is server behaviour and is **not determinable from source**. INFERENCE: after a server-side
  delete, the listener's exit code, whether it relaunches, and whether it deletes local config all depend
  on undocumented server responses.
- **F5.5 `run.cmd` relaunch loop.** `run-helper.cmd.template`:
  - exit 0 → stop;
  - exit 1 → stop;
  - exit 2 → wait about 5 s, then `run.cmd` relaunches;
  - exits 3 and 4 (update) → relaunch;
  - exit 5 (session conflict) → stop.

  (`src/Misc/layoutroot/run.cmd`, `run-helper.cmd.template`; return codes in `Constants.cs`.)
- **F5.6 Job acquisition.** In the broker flow, a job message is acknowledged best-effort and then acquired
  with `GetJobMessageAsync`. 404/409/422 on acquire are skipped (Runner.cs L680–758). INFERENCE: the server
  decides assignment before the listener acquires, so a gap exists between server assignment and local
  `busy`.
- **F5.7 No labels at session creation.** The listener's session carries agent ID/name/version/OS.
  Labels are set at configuration and stored server-side. INFERENCE: label changes made via REST persist
  across listener restarts.

---

## 6. Upstream / external evidence

### 6.1 GitHub REST — self-hosted runners (FACT, docs fetched 2026-09-27)

- `DELETE /repos/{owner}/{repo}/actions/runners/{runner_id}`: "Forces the removal of a self-hosted runner
  from a repository. You can use this endpoint to completely remove the runner when the machine you were
  using no longer exists." Responses: **204**, and **422** "Validation failed, or the endpoint has been
  spammed." **No documented busy refusal.**
- `GET …/runners/{runner_id}` and `GET …/runners`: fields include `id, name, os, status, busy, labels[]`
  (each with `id, name, type`), `ephemeral, version`.
- `DELETE …/runners/{runner_id}/labels/{name}`: "Remove a custom label … Returns the remaining labels …
  returns a 404 Not Found status if the custom label is not present." Responses 200/404/422.
- `PUT …/runners/{runner_id}/labels`: "Remove all previous custom labels and set the new custom labels."
  `POST …/labels` adds labels. Responses 200/404/422.
- `DELETE …/runners/{id}/labels/{name}` and `POST …/labels` are idempotent in outcome: the final label set
  is readable afterwards.

### 6.2 GitHub Actions routing and lifecycle (FACT, docs)

- "when you specify an array of labels, jobs will be queued on runners that have all the labels that you
  specify."
- "If GitHub finds an online and idle runner that matches the job's `runs-on` labels and groups, the job
  is then assigned and sent to the runner." "If the job remains queued for more than 24 hours, the job
  will fail."
- "If a job fails or is skipped, all jobs that need it are skipped unless the jobs use a conditional
  expression that causes the job to continue." "A job that is skipped will report its status as
  'Success'."
- Context availability: `jobs.<job_id>.if` may use `github, needs, vars, inputs`; `runs-on` may use
  `github, needs, strategy, matrix, vars, inputs`.
- "A self-hosted runner is automatically removed from GitHub if it has not connected to GitHub Actions for
  more than 14 days." (ephemeral: 1 day)
- "If you do not perform a software update within 30 days, the GitHub Actions service will not queue jobs
  to your runner."
- Re-runs "use the same `GITHUB_SHA` … and `GITHUB_REF`"; they use the privileges of the initial actor.
  They are allowed up to 30 days after the initial run, at most 50 times.
- List workflow runs: `status` filter values are `completed, action_required, cancelled, failure,
  neutral, skipped, stale, success, timed_out, in_progress, queued, requested, waiting, pending`. Up to
  1,000 results when filtering by status.

### 6.3 Documented gaps (explicitly NOT guaranteed by docs)

- **When `vars` values are resolved** (at run creation, at job `if` evaluation, or at re-run) is **not
  documented**. The only related statement is "The total combined size limit for organization and
  repository variables is 256 KB per workflow run". The protocol is designed not to depend on it (§9.4).
- **Linearizability of label changes versus in-flight assignment** is not documented.
- **Busy-refusal of DELETE** is not documented. The docs say "Forces".
- Whether re-running failed jobs re-runs dependents, and whether re-runs see updated variables, is not
  stated on the re-run page.
- The required GitHub App permission names for label and variable endpoints were not shown by the doc
  fetch. INFERENCE: label endpoints need repository **Administration: write**, which the product already
  uses for registration tokens. Variables need the repository **Variables** permission. Verify at
  implementation.

### 6.4 ARC graceful stop (FACT as ARC source; not proof for this runner)

`actions/actions-runner-controller` HEAD `2fb29e06e53be7bcd65e5657765cbd01546ae4e5`, file
`controllers/actions.summerwind.net/runner_graceful_stop.go` (blob `623dd4b6c557025373b47f1204471c46bd4b1016`):

- `unregisterRunner` (L432–458) calls RemoveRunner by ID and states: "we learned that RemoveRunner already
  has an ability to prevent stopping a busy runner … if it returned 200 you're guaranteed that the runner
  will not automatically come back". The example error is `422 … Runner "…" is still running a job`.
- `ensureRunnerUnregistration` (L182–263) treats 422 as `runnerBusy`. It annotates, then requeues after a
  delay for static runners and waits for completion for ephemeral ones. 403 is treated as already
  unregistered. A rate-limit error gets a long retry.
- The note at L446–450 says list responses "are now cached for 60 seconds … controlled by the
  Cache-Control response header".

**What generalizes:** DELETE-by-ID is a plausible server-side compare-and-set "remove if not busy", and
list data can be about 60 s stale. **What does not:** ARC kills a whole Kubernetes pod and never reuses
the registration. It runs in org/enterprise scopes with its own credentials, ARC's "guarantee" is an
observation rather than a documented contract, and ARC has no multi-job request affinity requirement.
DELETE also does not fence the idle gap between jobs of one run (R-c). **ARC behaviour is not proof for
this portable Windows runner or this GitHub App.**

---

## 7. Candidate protocols

### A. Request/workflow admission fence + drain + stop (without a runner fence)

Close admission (variable in `admit` `if`), wait for zero non-terminal runs, then stop.

- Fences R-a and R-c/R-d: new runs create no self-hosted jobs, and accepted runs finish all four jobs
  before the stop.
- **Does not fence R-e** (a re-run creates jobs without passing admission) or R-g (propagation). Between
  the last drain read and the stop, a new job can be assigned and then killed by the tree-kill Stop Now.
- **Verdict:** necessary, but not sufficient alone.

### B. Exact-ID server DELETE as the fence

DELETE the exact ID. On 422, stay online and retry later. On 204, the listener should exit on its own.

- Idle case: after 204 the registration is gone. INFERENCE (strong): a nonexistent runner cannot be
  assigned work.
- **Does not fence R-c:** the runner is idle between jobs, so DELETE can succeed mid-request. The
  remaining jobs then queue for any `grl-exec` runner (I3 violation) or wait up to 24 h.
- 422-while-busy is ARC-observed and undocumented ("Forces the removal").
- Listener exit path, exit code and local config deletion depend on which server error appears
  (F5.3–F5.5).
- **Destroys the registration:** every switch-back needs a new registration token, `config.cmd`, a new
  numeric ID and fresh credentials. The installer refuses an existing root (advisory D4), and the recovery
  evidence ID changes every time.
- **Verdict:** needs A anyway. It changes the owner's two-registration model. It is not needed once a
  non-destructive runner fence exists.

### C. Both registered, routing switch only (variable in `runs-on` or per-laptop labels)

- Moving the routing target does not un-route jobs already queued for the old target. In-flight runs get
  downstream jobs routed to the new laptop (I3 violation). It needs an E `runs-on` change plus lint change.
- **Verdict:** moves the race rather than fencing it. It still needs A + drain. Rejected as the primary
  mechanism.

### D. `busy=false` + Stop Now

- `busy=false` is a snapshot of an unbounded, growing set of assignable work. Between read and kill, an
  assignment can arrive (R-b) or a gap job can queue (R-c). The product stop is a tree kill (§2.2).
- **Verdict:** rejected. It violates I1 by construction.

### E. Two-layer fence (recommended) — A + exact-ID label fence + frozen-set verification

This is A, plus `DELETE …/labels/grl-exec` on the exact ID after the drain, a re-verification over a
settle window, and only then the stop. Activation is the reverse. Details in §9–§12.

### E′. Two-label admission (fallback if the variable permission is declined)

Admit gets `runs-on: [self-hosted, Windows, X64, grl-exec, grl-admit]`. Removing `grl-admit` from the
active runner closes admission: new admit jobs queue rather than skip. Then drain, then remove
`grl-exec`. It needs no new App permission and loses no requests.

Weaknesses:

- Both layers are the **same mechanism**, so their failure modes are correlated.
- Queued requests can wait up to 24 h if a switch is abandoned.
- The drain predicate must classify individual jobs: a queued admit with no runner is allowed, anything
  else waits. That depends on how quickly `runner_id` becomes visible.

Kept as documented fallback only.

---

## 8. Rejected approaches and why

| Approach | Reason |
|---|---|
| D `busy=false` + Stop Now | Snapshot race R-b/R-c. The tree kill cancels without reporting. Violates I1. |
| B DELETE as primary fence | Doesn't fence R-c. Undocumented 422 semantics. Destroys the registration each switch. Undetermined listener exit/config path. Changes D30 (§14). Kept only for stale **offline** cleanup (existing `DeleteStaleAsync`). |
| C variable `runs-on` routing | Moves the race. Queued jobs keep their target. I3 split. Larger E/lint change. |
| Workflow disable (`PUT …/workflows/{id}/disable`) | The effect on already-created runs' pending jobs is undocumented. Coarse. CT explicitly warned not to equate it with a fence. It would also silence non-request runs. |
| `concurrency:` group | Serializes runs but does not prevent assignment. Pending runs are *replaced* (a request would be lost). |
| Runner groups | An org/enterprise feature. The execution repo is a personal-account repository (INFERENCE: not available). It would not fence in-flight jobs anyway. |
| `--ephemeral` / `--once` | One job per registration. A four-job request would need four registrations. Contradicts the portable persistent model. |
| `GRL_AUTHORIZED_ACTORS=[]` as the switch | Zero E change, and safe because `author_association == 'OWNER'` is an independent gate. But it overloads a security policy variable, needs exact journaled restore, and gives an unclear audit trail. Acceptable only as a manual interim if the owner prefers (§20 OD-B alt). |
| Asking the owner to stop posting | Not a technical fence (CT addendum). |

---

## 9. Recommended protocol (PROPOSED)

### 9.1 GitHub-side state (authoritative, readable, idempotent)

- `V` = repository variable `GRL_ADMISSION`. Open iff the value is exactly `open`. Missing means closed
  (fail-closed).
- `H` = the set of runners whose labels contain `grl-exec`. It is read from the runner list, which the
  product must extend to parse labels.
- `N` = the set of non-terminal workflow runs in the repository. It is read by querying each of `queued,
  in_progress, requested, waiting, pending, action_required` with the `status` filter. This is
  repository-wide rather than workflow-scoped, so a re-run of an old run or an unexpected workflow is
  still seen. Age ordering does not hide an old re-run because the query filters by status.
- `O(r)`, `B(r)` = online/busy of runner `r`, by exact ID.

Local state: product-owned process handle; persisted switch journal (§15); existing recovery evidence.

### 9.2 Timing constants (PROPOSED defaults, witness-tunable)

| Constant | Default | Meaning |
|---|---|---|
| `T_settle` | 90 s | ≥ ARC-observed 60 s list cache + propagation margin |
| `T_quiet` | 60 s | Separation between two confirming reads |
| `T_fresh` | 30 s | Maximum age of the last confirming read before the stop |
| `T_leak` | 2 × `T_settle` | Minimum age before a queued, runner-less run after the fence may be classified as a benign leak |
| Poll interval | 15–30 s | Backoff on 403/429 per the `Retry-After` / `X-RateLimit-Reset` headers |

### 9.3 Why each layer is needed

- The **label fence alone** splits requests mid-flight (R-c → I3 violation).
- The **admission gate alone** leaves R-e/R-g able to create a job that the stop then kills (I1
  violation).
- **Drain before the label fence** ensures no accepted request has pending downstream jobs at fence time.
- **Verification after the label fence** turns `busy=false` from an unbounded snapshot into an
  observation of a *frozen, monotonically completing* set: `J_X` = jobs assigned to X before the fence.
  INFERENCE: after the label removal is effective, `J_X` cannot grow. Every job in `J_X` belongs to a run
  that stays non-terminal until the job ends. So "N empty and X not busy, confirmed twice across
  `T_quiet` after `T_settle`" implies `J_X` has completed, up to API staleness bounded by the settle
  window.

### 9.4 Independence from undocumented variable timing

- If `vars` is snapshotted at run creation, a run created just before the close runs normally. It is in
  `N`, and the drain waits for it.
- If a run created after the close still sees `open` because of propagation, it is also in `N`. The drain
  waits. If it appears after the label fence, its `admit` cannot be assigned to X and it waits for the
  next active runner. That runner admits it normally, which is correct. The event is recorded as an
  `ADMISSION_LEAK` witness finding.
- Therefore **I1 never depends on variable timing**, and the variable only provides I3 for new requests.

### 9.5 Re-runs (R-e)

A re-run creates jobs without a new comment and bypasses the `admit` gate when admit already succeeded.
It is covered as follows:

- Before the label fence, it is in `N`, so the drain waits.
- After the label fence, it cannot be assigned to X, so I1 holds. Its jobs wait for the next active
  runner. A re-run `execute` there would be an I3 split. **Detection:** any run in `N` with
  `run_attempt > 1`, or whose admit completed on X, is flagged. If this happens before the stop, **roll
  back** the label so the run completes on X.
- The owner UI warns: "Do not re-run GRL workflows during a switch." Admission also still refuses
  unauthorized re-run triggering actors (`UNAUTHORIZED_RERUN`).

---

## 10. Exact state machine

The global state is **derived from GitHub** (`V`, `H`, `N`, `O`, `B`) plus local ownership. The journal
records intent and outcome but never overrides a fresh GitHub read.

| State | Definition |
|---|---|
| `ACTIVE(X)` | `V=open`, `H={X}`, `O(X)` |
| `ACTIVE_DEGRADED(X)` | `V=open`, `H={X}`, `¬O(X)`. Requests queue. No switch action is automatic. |
| `CLOSING(X)` | Journal intent to close; `V` not yet confirmed closed |
| `DRAINING(X)` | `V≠open`, `H={X}`, not yet quiet (N≠∅, or quiet not yet confirmed twice) |
| `QUIESCENT(X)` | `V≠open`, `H={X}`, `N=∅` and `¬B(X)` confirmed twice across `T_quiet` after `T_settle` |
| `FENCING(X)` | Journal intent to remove `grl-exec` from X; not yet confirmed |
| `FENCED_VERIFYING(X)` | `V≠open`, `H=∅`, X online, frozen-set verification in progress |
| `FENCED_QUIESCENT(X)` | Verification passed; the last read is at most `T_fresh` old |
| `RELEASED` | `V≠open`, `H=∅`, no owned process; X offline, or online label-less standby |
| `ACQUIRING(Y)` | `V≠open`, `H=∅`, `N` has no started job; Y started, waiting for online |
| `ARMING(Y)` | Y online, not busy, label-less; journal intent to add `grl-exec` |
| `ARMED(Y)` | `V≠open`, `H={Y}`, `O(Y)` |
| `OPENING(Y)` | Journal intent to set `V=open` |
| `SWITCH_BLOCKED` | An invariant violation or precondition failure observed. No mutation is performed; the reason is shown. |
| `RECOVERY_REQUIRED` | Local uncertainty (possible unowned survivor, emergency Stop Now used, journal/GitHub contradiction, registration missing) |

`SWITCH_BLOCKED` reasons include:

- `TWO_HOLDERS`: `|H|>1`;
- `HOLDER_NOT_SELF`;
- `DUPLICATE_NAME`;
- `ID_MISMATCH`;
- `REGISTRATION_ABSENT`;
- `REMOTE_UNAVAILABLE`;
- `SPLIT_RISK`: a run in `N` was admitted on another runner;
- `DRAIN_TIMEOUT_NOTICE` (informational; the drain continues).

**Transitions** (every mutation is preceded by a fresh exact-ID read and followed by a verification read):

| # | From → To | Preconditions (fresh) | Operation | Verification | Rollback / on failure |
|---|---|---|---|---|---|
| T1 | `ACTIVE(X)` → `CLOSING` → `DRAINING(X)` | Product owns X's process; exactly one runner named X with persisted ID; `H={X}`; `O(X)` | Journal intent; `PATCH` variable `GRL_ADMISSION=closed` | `GET` variable = `closed` | Uncertain result → re-read. Unreadable → stay `CLOSING`, retry only; no further step. Abort → set `open` (verify). |
| T2 | `DRAINING(X)` → `QUIESCENT(X)` | `T_settle` elapsed since T1 was confirmed | Poll `N`, `B(X)`, `H` | `N=∅ ∧ ¬B(X) ∧ H={X}` on two reads ≥`T_quiet` apart | Any `N≠∅` restarts the quiet timer. No upper bound forces action; after the owner-visible notice the owner may *Abort* (reopen) or keep waiting. Never stop. |
| T3 | `QUIESCENT(X)` → `FENCING` → `FENCED_VERIFYING(X)` | Re-read: `N=∅`, `H={X}`, exact ID/name unique | Journal intent; `DELETE …/runners/{id}/labels/grl-exec` | `GET …/runners/{id}`: ID+name match, `grl-exec` absent, `H=∅` | 200/404 → verify by read. 422/transient/timeout → re-read the labels and decide. Unreadable → stay `FENCING` (the runner still works or is fenced; either is safe). No stop. |
| T4 | `FENCED_VERIFYING(X)` → `FENCED_QUIESCENT(X)` | `T_settle` after T3 was confirmed | Poll `N`, `B(X)`, `H`, X labels | X lacks `grl-exec` ∧ `¬B(X)` ∧ no job in `N` has `runner_id = X` ∧ no run in `N` was admitted on X, on two reads ≥`T_quiet` apart; last read ≤`T_fresh` old. `H={Y}` for another runner Y is acceptable here (concurrent acquire, §10.1). | If `N≠∅` and any run has a started job on X, admit completed on X with pending downstream, or `run_attempt>1` → **ROLLBACK_FENCE**: `POST` label `grl-exec` to X (verify), go to `DRAINING(X)`. If `N` holds only runs with every job queued, no runner assigned, age ≥`T_leak` and `¬B(X)` → record `ADMISSION_LEAK`; continue (they will be served by the next active runner). |
| T5 | `FENCED_QUIESCENT(X)` → `RELEASED` | Fresh read ≤`T_fresh`: X lacks `grl-exec`, `¬B(X)`, no job in `N` bound to X (as in T4) | Stop the owned process (existing tree kill). Optional alternative: keep X online as a label-less standby. | Poll until `¬O(X)` (or standby confirmed label-less) | Stop failure → `RECOVERY_REQUIRED` (possible survivor). The label stays removed; I2 holds. |
| T6 | `RELEASED` → `ACQUIRING(Y)` | `V≠open`; `H=∅`; no run in `N` has a started job; Y exact ID+name registered, unique, offline; no unowned survivor evidence | Start Y's existing registration (resume `run.cmd`). Y has no `grl-exec`. | `O(Y) ∧ ¬B(Y)`, Y labels lack `grl-exec` | Online timeout → leave the process, report `Degraded` (never kill implicitly). `H≠∅` → `SWITCH_BLOCKED(HOLDER_NOT_SELF)`, offer remote release (§11.3). |
| T7 | `ACQUIRING(Y)` → `ARMING` → `ARMED(Y)` | Fresh: `H=∅`, `O(Y)`, `¬B(Y)` | Journal intent; `POST …/runners/{Y}/labels ["grl-exec"]` | `H={Y}` exactly | Uncertain → re-read. If `H` contains another runner → remove Y's label, `SWITCH_BLOCKED(TWO_HOLDERS)`. |
| T8 | `ARMED(Y)` → `OPENING` → `ACTIVE(Y)` | Fresh: `H={Y}`, `O(Y)` | Journal intent; `PATCH GRL_ADMISSION=open` | `GET` = `open` | Failure leaves admission closed, which is safe. Retry or leave `ARMED`. |
| T9 | any pre-T5 → `ACTIVE(X)` (Abort release) | Owner action | If the label was removed, `POST grl-exec` to X (verify `H={X}`); then `V=open` | Reads | — |

Nothing in the table stops, deletes or reopens without both a fresh precondition read and an explicit
owner-initiated switch action. There is no scheduled or automatic handoff.

### 10.1 Refinements found while attacking the design

- **Offline-holder drain (T2′).** Remote release and releasing a paused holder can leave X offline:
  `¬O(X)` confirmed on two reads, and no owned process. In that case `N` may contain requests queued while
  admission was open, and they can never be served by X. The strict `N=∅` would then never be reached.

  T2′ replaces the drain predicate with: *every run in `N` is entirely unstarted* (no job
  in_progress/completed, no `runner_id`) and is aged ≥`T_leak`.

  T3 may then remove `grl-exec` from the offline X. Those queued runs are later admitted whole by the next
  active runner. That preserves I3, because none of their jobs started, and I1, because an offline,
  label-less X runs nothing.

  If X comes back online, it is label-less. A started job on an offline X (crash mid-job) still blocks
  until the server fails that job.
- **Concurrent release and acquire.** The two laptops may act at the same time: office releasing, personal
  acquiring or remote-releasing. This is safe:
  - label removal is idempotent;
  - acquire (T6/T7) requires `H=∅` and no started work;
  - office's T4/T5 check only that **X** lacks the label and is idle.

  A runner Y that newly holds the label cannot cause work to be assigned to X.
- **External reopen.** If `V=open` is observed while the local journal is in `DRAINING`…`FENCED_*`, the
  state becomes `SWITCH_BLOCKED(ADMISSION_REOPENED_EXTERNALLY)`. No fence and no stop happen. The owner
  chooses to abort, or to close again.
- **Planned pause is not a switch.** The GRL-015 planned pause (warned Stop Now while holding the label)
  remains available. It can cancel a job that races in. A **Release on the same machine followed later by
  Acquire** is the new safe pause. It costs admission being closed meanwhile.

---

## 11. Office → personal sequence (release on office, acquire on personal)

The two halves run on different laptops, often in different places. They coordinate **only through
GitHub state**. The intermediate state `RELEASED` (admission closed, no holder) is a normal, safe resting
state.

### 11.1 Release (office product, runner `grl-office`, persisted ID)

1. Fresh reads (exact identity, `H={grl-office}`, online, owned process). → T1: close admission and verify.
2. T2: drain until `N=∅` and not busy, confirmed twice. The UI shows the outstanding run IDs and job
   names.
3. T3: remove `grl-exec` from the exact ID and verify the labels by `GET`.
4. T4: frozen-set verification, with rollback-to-draining on any started work.
5. T5: stop the owned process and verify offline. The journal records `RELEASED` with the timestamp,
   runner ID, and confirmation that admission is closed and the label is absent.
6. The UI states: "No runner is active. Requests posted now are not admitted (no ACK). Activate the other
   laptop to reopen."

### 11.2 Acquire (personal product, runner `grl-personal`, persisted ID; requires prior enrollment §14)

1. Fresh reads: `V≠open`, `H=∅`, no started job in `N`, exact own registration unique and offline.
2. T6: start the existing registration label-less and verify online/idle.
3. T7: add `grl-exec` and verify `H={grl-personal}`.
4. T8: set `GRL_ADMISSION=open` and verify.
5. Optional harmless js-smoke confirmation.

### 11.3 Remote release (forgot to release on office)

From the personal product, when `H={grl-office}`:

- Apply T1 (close) → T2 (drain; T2′ if office is confirmed offline) → T3 (remove `grl-exec` from office
  ID 3 remotely) → T4 (verify office lacks the label, is not busy, and has no started work).
- **Do not stop the office process** (impossible remotely, and unnecessary). Office stays online as a
  label-less standby. INFERENCE: it cannot receive GRL work.
- Then T6–T8 for personal. On return, the office product shows "inactive (released remotely)" and may stop
  its process. The label is absent, `¬B` is re-verified, and the stop is safe.

The product on office must never re-add `grl-exec` except through an explicit Acquire.

---

## 12. Personal → office sequence

This is symmetric. Release on personal (T1–T5), then acquire on office (T6–T8) using the **existing ID 3
registration** (resume path, no configure). Rollback to office after a failed personal activation is just
"Acquire on office". Both paths reuse the planned-pause resume proven in GRL-015.

**One-machine full cycle:** release office → acquire office. This exercises every transition without
enrolling personal and is the basis of live witness W3 (§18).

---

## 13. Failure / recovery matrix

"Auth" = authoritative state. "Never" = what must never happen automatically.

| Scenario | Auth | Safe to continue? | Recovery action | Never |
|---|---|---|---|---|
| Idle → assignment between check and stop | Runs/jobs after fence | Yes, because the fence precedes the check | T4 detects a started job → rollback label → drain | Stop on a `busy=false` snapshot taken before the fence |
| Request just before fence (T1) | Run in `N` | Yes | T2 waits for all four jobs | Label fence while `N≠∅` |
| Request just after fence | `V` + run conclusion (admit skipped) | Yes | Requester sees no ACK and re-posts after reopen | Fabricate an ACK or refusal |
| Admit done, execute pending | Run in `N` | Yes | T2 waits | Treat the idle gap as done |
| Execute / report / verdict pending | Run in `N` | Yes | T2 waits until the run is `completed` | Stop between jobs |
| Overlapping requests | `N` | Yes | Waits for all | Assume one run == one request finished |
| Active job at release start | `B(X)`, `N` | Yes | T2 waits (the job runs to completion) | Stop Now / DELETE |
| `busy=false` race | Frozen set after fence | Yes | Two reads across `T_quiet` after `T_settle` | Single-read decisions |
| Network failure during fence | Label re-read | Pending | Stay `FENCING`; retry the read | Assume success/failure |
| API timeout, uncertain mutation | Re-read variable/labels | Pending | Idempotent retry after read | Double-apply without reading; stop |
| Rate limiting (403/429) | Headers | Pending | Honour `Retry-After`/reset; the quiet timer restarts after a gap | Skip verification reads to save quota |
| Runner absent unexpectedly (ID gone) | Runner list | Blocked | `REGISTRATION_ABSENT` → `RECOVERY_REQUIRED`. Drain must still reach `N=∅`. Re-registration is a separate owner-confirmed action. | Adopt a same-name runner with a different ID |
| Listener exits unexpectedly mid-drain | `N`, `O(X)` | Yes (drain) | In-flight job fails on the server side (not caused by the switch). Drain continues to terminal. Journal it. | Restart the listener automatically during release |
| App crash mid-switch | GitHub state + journal | Resume | On relaunch: re-derive state from GitHub, show the resumable step, require an owner click | Auto-continue to stop or auto-reopen |
| Wrong UI action (emergency Stop Now during drain) | Owned process gone | Degraded | Record `RECOVERY_REQUIRED(EMERGENCY_STOP)`; possible cancelled job is flagged; admission stays closed | Hide the cancellation risk |
| New laptop fails after old is fenced | `H=∅`, `V` closed | Yes (safe outage) | Acquire on old laptop (T6–T8) | Open admission with `H=∅` or with Y offline |
| Rollback to old laptop | Same as acquire | Yes | T6–T8 on old ID | Re-register old laptop |
| Duplicate names | Runner list | Blocked | `DUPLICATE_NAME`; owner inspects; stale **offline** duplicate may be removed by existing `DeleteStaleAsync` | Choose one by name |
| Numeric ID mismatch vs journal | Runner list | Blocked | `ID_MISMATCH` → `RECOVERY_REQUIRED` | Rewrite journal to match |
| Stale recovery evidence (e.g. ID 2) | Runner list | Blocked | Refuse; fresh identity resolution required | Target historical ID 2 |
| Two holders of `grl-exec` | Runner list | Blocked | `TWO_HOLDERS`: close admission (safe), owner chooses which label to remove after drain | Remove a label from a busy runner without drain / auto-choose |
| Admission reopened by someone else mid-release | `V` read | Blocked | `ADMISSION_REOPENED_EXTERNALLY`; owner aborts or closes again | Fence or stop while `V=open` |
| Holder offline (paused) when release starts | `O(X)`, jobs | Yes | T2′ (§10.1): fence the offline X once `N` holds only unstarted runs | Wait forever; resume X just to drain it |
| `ADMISSION_LEAK` observed | Runs | Yes | Record as a witness failure of layer 1. The leaked run is served by the next active runner. | Ignore silently |
| Re-run during switch | Run `run_attempt>1` | Yes / rollback | Rollback the label before the stop; warn | Stop with a re-run non-terminal |
| Inactive registration auto-removed (14 days) | Runner list | Blocked for that laptop | Owner-confirmed fresh registration (label-less) | Silent re-registration |
| Runner version drift on start | CLI probe | Blocked | Existing `UnsupportedVersion` gating (D22) | Downgrade/guess |

---

## 14. Registration-model implications

**Both persistent registrations survive.** Office ID 3 and a future personal ID each keep their own
credentials on their own laptop. No credential is copied, which satisfies D30. A switch uses only label
and variable operations plus local stop/resume.

Refinements the owner must see:

1. **OWNER DECISION REQUIRED — D30 wording.** D30 says "ONE ACTIVE AT A TIME while both share label
   `grl-exec`". This protocol requires **`grl-exec` to be held only by the active runner**. The inactive
   registration keeps the default labels (`self-hosted, Windows, X64`) and receives no GRL work.
   - Usability: switching is label + variable operations; no re-registration.
   - Safety: stronger, because server routing enforces single-active rather than process discipline alone.
   - Cost: two label API calls per switch.
   - Credentials: unchanged.
   - Recovery: a missing label is fixed by one idempotent call.
2. **Personal enrollment must register without `grl-exec`.** The current product always configures with a
   label. Registering `grl-personal` with `grl-exec` would violate I2 immediately while office is active.
3. **14-day auto-removal (FACT).** An inactive laptop that never connects for more than 14 days loses its
   registration. Mitigation (PROPOSED): a label-less **standby start** of the inactive runner at least
   every ~10 days is safe by the label argument. Otherwise, accept owner-confirmed re-registration
   (§20 OD-D).
4. **30-day update rule (FACT)** applies to both laptops. It interacts with the pinned-version gating
   (D22) and is not changed here.
5. **No other workflow may target `[self-hosted]` alone** in the execution repo. A label-less standby is
   inert only because every GRL job requires `grl-exec`. INFERENCE: the current tree has exactly one
   workflow. The template lint should keep enforcing this.

**If the owner instead chooses Candidate B (DELETE):**

> OWNER DECISION REQUIRED (not recommended): session-scoped registration — every deactivation deletes
> the active registration and every activation registers fresh (new numeric ID, new credentials, local
> config recreated).

- Usability: each switch needs a registration token + `config.cmd` + an existing-root reinstall path.
- Safety: equal only when combined with A.
- Recovery: an uncertain 204/422 leaves an unknown listener exit path.
- Identity: a new ID on every switch.

---

## 15. Minimal implementation delta (not implemented here)

**Required (M):**

- **M1 — E template + private deploy (one clause).**
  - Add `vars.GRL_ADMISSION == 'open' &&` as the first clause of the `admit` `if:`. There is no `||` and
    no `true` literal, so it is compatible with lint L28.
  - Add a `requireText` for the clause in `tools/lint.mjs`, plus tests.
  - README bootstrap: a fifth variable.
  - **Rollout order:** create `GRL_ADMISSION=open` in the private repo **before** deploying the workflow
    change, otherwise admission is closed (a fail-closed outage). Then a js-smoke confirmation.
  - No `runs-on` change, no Core/schema change.
- **M2 — `GitHubRunnerAdministration`:**
  - parse `labels` in `ListAsync`;
  - `GetAsync(id)`, exact, with 404 → `Absent`;
  - `RemoveLabelAsync(id, "grl-exec")` and `AddLabelAsync(id, "grl-exec")`, classifying 200/404/422/
    transient, with a mandatory verification read;
  - keep `DeleteStaleAsync` offline-only.
- **M3 — Actions observation adapter:**
  - `ListNonTerminalRunsAsync()` over the six statuses, repository-wide, bounded pages;
  - `ListRunJobsAsync(runId, filter=latest)` returning job name/status/`runner_id`/`runner_name`;
  - `Get/SetAdmissionAsync()` for the `GRL_ADMISSION` variable.
- **M4 — `SwitchCoordinator`**: a pure state machine per §10 over injected M2/M3, the lifecycle, a clock
  and a delay. Each mutation gets a journal intent record *before* it and an outcome record *after* it.
  All decisions come from fresh reads.
- **M5 — Persisted non-secret switch journal** beside the existing recovery store: repo, runner name + ID,
  state, last confirmed reads, intent/outcome, timestamps. It holds no tokens, paths of other machines or
  usernames.
- **M6 — Lifecycle changes:**
  - `DeactivateAfterFenceAsync`: stops only when the coordinator hands it a fresh `FENCED_QUIESCENT`
    proof; otherwise refuses.
  - Configure without a custom label for new enrollments.
  - The product never adds `grl-exec` outside Acquire.
  - Emergency Stop Now stays, warned, and records `RECOVERY_REQUIRED`.
- **M7 — UI:**
  - Release / Acquire / Abort release / Remote release;
  - authoritative-runner display (`H`, `V`, outstanding runs);
  - a "no re-runs during switch" warning.
- **M8 — Tests:** the deterministic simulator (§16) plus unit tests for M1–M7.

**Recommended hardening (H, optional):**

- **H1** — the admit step compares `RUNNER_NAME` with an optional `GRL_ACTIVE_RUNNER` variable and refuses
  with a new ACK reason. This binds admission to the exact runner name and gives defense in depth against
  `TWO_HOLDERS`.
- **H2** — a three-value `GRL_ADMISSION` (`open` / `draining` / `sealed`), where downstream jobs also
  require `≠ sealed` to cover re-runs. Deferred: it depends on the undocumented `vars` re-run timing.
- **H3** — a keep-alive standby reminder for the 14-day rule.

Files likely touched:

- `templates/execution-repo/.github/workflows/grl-dispatch.yml`
- `templates/execution-repo/tools/lint.mjs`
- `templates/execution-repo/tests/lint.test.mjs`
- `templates/execution-repo/README.md`
- `src/Grl.Integration/GitHubRunnerAdministration.cs`
- new `src/Grl.Integration/ActionsObservation.cs`
- new `src/Grl.Integration/SwitchCoordinator.cs` and `SwitchJournal.cs`
- `src/Grl.Integration/PortableRunnerLifecycle.cs`
- `src/Grl.Integration/LiveWizardComposition.cs`
- `src/Grl.App.Presentation/*` (UI states)
- `tests/Grl.Integration.Tests/*`

The private repo changes only by deploying the reviewed template (M1). App permission changes are §20.

---

## 16. Deterministic race-harness plan

Build a discrete-event **GitHub simulator**: runs, jobs with needs/if semantics, labels, the variable,
runner online/busy/session, assignment, and API read caches. Every event point is injectable:

- **Server events:**
  - comment → run created;
  - `if` evaluated, with a configurable variable-visibility lag (snapshot-at-creation or at-evaluation
    mode);
  - job queued;
  - assignment decision;
  - delivery to the listener with a lag;
  - acquire;
  - busy flip;
  - job end;
  - re-run;
  - label/variable mutation applied, with a lag;
  - reads served from a cache of age ≤ C.
- **Local events:** app crash/restart at every instruction, listener death, stop.
- **Faults:** mutation succeeds-but-times-out, 403/429, 5xx, list omission within the cache window.

Harness: enumerate all interleavings, bounded to 2 runners, ≤3 requests, ≤1 re-run and one fault per
trace. Checks run after every step:

- **I1:** no stop while any job is assigned to the stopped runner, or delivered but not acquired;
- **I2:** `|H| ≤ 1` always, and every switch-driven open happens with `|H|=1 ∧ online`;
- **I3:** all jobs of an admitted run share one runner, or a flagged `SPLIT_RISK` exists with no
  unflagged split;
- **I4:** every uncertain step yields pending/blocked;
- **I5:** every mutation's target ID equals the fresh-read ID.

Required scenario suites:

- fence vs new request;
- fence vs new assignment (delivery lag);
- all transitions between the four jobs;
- overlapping requests;
- active job;
- idle race;
- uncertain API result for each mutation;
- label removal racing assignment;
- re-run;
- process exit;
- app crash at each transition;
- rollback (T4 → `DRAINING`, T9 abort);
- remote release;
- registration absent, duplicate name, ID mismatch.

**Negative controls:** candidate D and label-fence-only must be *shown to fail* I1/I3 in the harness.
This proves the harness can detect the races.

Evidence level: `LOCAL_CHECKED` on a worker; not Windows or live.

Also: **source-contract proofs** (static, per exact runner tag) that
- `run` has no drain flag;
- F5.2 cancels on shutdown;
- F5.3/F5.4 map to the listed exit codes;

checked by a probe test that greps the pinned source/assemblies. Pattern: the existing
`Grl.RunnerContractProbe`.

## 17. Windows proof plan (synthetic/local, no live GitHub)

On the office laptop, without touching ID 3's root or process:

- build and run M8 on Windows;
- the product's `SwitchCoordinator` against a local fake GitHub HTTP handler, with a *fake* `run.cmd`
  child (a benign long-running process) in a scratch owned root. Prove:
  - stop happens only after `FENCED_QUIESCENT`;
  - a crash/reopen at each state re-derives and does not act;
  - the tree kill targets only the owned fake process;
  - the journal contains no secrets or absolute user paths.

Evidence: `LOCAL_CHECKED` (Windows qualifier, D24). Not `WINDOWS_TESTED`.

## 18. Later live acceptance plan (requires separate explicit authorization; js-smoke only)

Stop points are recoverable. The office runner is ID 3. `grl-personal` is not registered until W5.

| W | Setup | Action | PASS | BLOCKED/FAIL |
|---|---|---|---|---|
| W1 label-fence routing | ID 3 active, admission as today | Remove `grl-exec` from ID 3 (idle, `N=∅`); post one js-smoke; observe ≥ 5 min; re-add the label | Admit stays `queued` with no runner while the label is absent; after re-add it runs and the full PASS lands | FAIL: any job assigned to ID 3 without the label |
| W2 admission gate | M1 deployed, `V=open` | Set `V=closed`; post js-smoke; then `V=open`; post again | First run: all 4 jobs skipped, no ACK. Second: PASS | FAIL: an admit job created while closed |
| W3 one-machine cycle | M2–M7 build | Release office (T1–T5), then Acquire office (T6–T8), then js-smoke | Every transition's verification recorded; same ID 3; PASS | Any stop without a fresh `FENCED_QUIESCENT` proof = FAIL |
| W4 mid-request drain | M2–M7 build | Post js-smoke; start Release right after the ACK appears | T2 waits across all four jobs; release completes after verdict; no cancelled job | FAIL: stop or fence before run completion |
| W5 personal | Separate owner authorization (D30 + OD-A) | Enroll `grl-personal` label-less; release office; acquire personal; js-smoke; release personal; acquire office; js-smoke | Both PASS; `H` never >1 | — |

Outcome classes:

- **PASS** — every verification is satisfied.
- **BLOCKED** — a precondition is unmet and nothing was mutated. Publish the exact blocker.
- **FAIL** — an invariant was violated or a job was cancelled.
- **RECOVERY_REQUIRED** — uncertain local or remote state. Preserve it; no cleanup.

No live DELETE, no 422-busy experiment and no cancellation of real work is part of this plan.

---

## 19. Security / secret boundaries

- No new token types. The label and runner APIs use the existing App user token with Administration
  write. The variable API needs the Variables permission (OD-B).
- The journal and published evidence contain repo name, runner names, numeric IDs, run IDs and
  timestamps only. They contain no tokens, registration/remove tokens, `.credentials`, absolute paths,
  usernames or machine hostnames.
- A label-less online standby still executes nothing. The listener still holds its own credentials on
  its own laptop, as today.
- `author_association == 'OWNER'` and the actor allowlist remain unchanged. The admission variable only
  narrows admission.
- No hosted Actions (D19), no service/UAC/autostart, no cross-repo work.

## 20. Owner decisions required

- **OD-A.** Re-interpret D30: `grl-exec` is held **only by the active runner**. Both registrations persist.
  A personal enrollment registers without `grl-exec`.
- **OD-B.** Grant the GitHub App the repository Actions **Variables** permission (read/write) on the
  execution repo only, so the product can close/open and verify admission. *Alternatives:*
  - the owner toggles `GRL_ADMISSION` manually in Settings and the product only verifies it (still needs
    read access);
  - E′ two-label admission (no new permission; weaker, §7);
  - interim `GRL_AUTHORIZED_ACTORS=[]` manual switch (§8).
- **OD-C.** Accept that requests posted while admission is closed are **skipped** (no ACK) and must be
  re-posted after reopen.
- **OD-D.** 14-day policy: periodic label-less standby start of the inactive laptop (PROPOSED), or accept
  owner-confirmed re-registration after auto-removal.
- **OD-E.** Allow **remote release** (§11.3): the personal product removes office's label while office's
  listener stays online label-less.
- **OD-F.** Authorize a source implementation packet (M1–M8) with independent review, then live witnesses
  W1–W4 on office ID 3. W5 requires personal-enrollment authorization.
- **Not recommended:** session-scoped DELETE/re-register model (§14).

## 21. Open questions (do not block the design; each has a witness or harness hook)

1. When are `vars` values resolved for job `if` and re-runs? The design is independent of this (§9.4);
   W2 measures lag.
2. Is label removal linearizable with in-flight assignment? The design tolerates it via T4 rollback; W1
   measures routing.
3. What is the actual staleness bound of runner/run list responses today? `T_settle` is tunable.
4. Do runs whose jobs are all skipped conclude `skipped` promptly, or linger `queued`? This affects drain
   latency only.
5. Does re-running failed jobs re-run dependents (docs silent)? The design treats any re-run as a
   potential job creator.
6. What is the exact GitHub App permission name for variable and label endpoints? Verify at
   implementation.
7. (Not used) Is DELETE busy refusal a stable contract? It is irrelevant to the recommended protocol.

## 22. Evidence / source appendix

Project (read 2026-09-27):

- Public `bohanyt/github-runner-local` main `02c1e167d0d232d4b76a1bd6ba1beb87c17be9bd`: `AGENTS.md`,
  `docs/CURRENT.md`, V50 handoff, `docs/control-tower/PROTOCOL.md`, `docs/DECISIONS.md` (D30, D31, D32),
  `src/Grl.Integration/{GitHubRunnerAdministration,PortableRunnerLifecycle,PortableRunnerProcess,PortableRecoveryStore}.cs`,
  `templates/execution-repo/**`.
- Private `bohanyt/github-runner-local-exec` main `2c8da8834e785ba012001b9427bd68380401bec7`. The full
  tree is blob-identical to the public template (34 files).
- Issues: #18 body plus comments `5828869070` and `5852242889`; #21 `5852242726`; #1 `5828798876`,
  `5852239423`, `5852248513`.

actions/runner `v2.337.0` = `397b032cbf865e9c3ddfab89d533ec19325e1273`
(https://github.com/actions/runner/tree/v2.337.0):

- `src/Runner.Listener/Runner.cs`:
  - L359–391 CtrlCHandler;
  - L407–900 RunAsync;
  - L680–758 broker job acquire;
  - L823–830 RunnerNotFound catch;
  - L852–880 shutdown/cleanup.
- `src/Runner.Listener/MessageListener.cs` L298–383, L499–516.
- `src/Runner.Listener/BrokerMessageListener.cs` ~L310–360, ~L445–460.
- `src/Runner.Listener/Program.cs` ~L120–160.
- `src/Runner.Listener/JobDispatcher.cs` L206–250.
- `src/Misc/layoutroot/run.cmd`, `src/Misc/layoutroot/run-helper.cmd.template`.
- `src/Runner.Common/Constants.cs` (`ReturnCode`).

actions/actions-runner-controller HEAD `2fb29e06e53be7bcd65e5657765cbd01546ae4e5`:

- https://github.com/actions/actions-runner-controller/blob/2fb29e06e53be7bcd65e5657765cbd01546ae4e5/controllers/actions.summerwind.net/runner_graceful_stop.go
  (blob `623dd4b6c557025373b47f1204471c46bd4b1016`), L182–263 and L432–458.

GitHub documentation (fetched 2026-09-27):

- https://docs.github.com/en/rest/actions/self-hosted-runners
- https://docs.github.com/en/rest/actions/variables
- https://docs.github.com/en/rest/actions/workflow-runs
- https://docs.github.com/en/actions/reference/workflows-and-actions/contexts
- https://docs.github.com/en/actions/reference/workflows-and-actions/variables
- https://docs.github.com/en/actions/how-tos/write-workflows/choose-where-workflows-run/choose-the-runner-for-a-job
- https://docs.github.com/en/actions/how-tos/write-workflows/choose-what-workflows-do/use-jobs
- https://docs.github.com/en/actions/how-tos/write-workflows/choose-when-workflows-run/control-jobs-with-conditions
- https://docs.github.com/en/actions/reference/runners/self-hosted-runners
- https://docs.github.com/en/actions/how-tos/manage-runners/self-hosted-runners/remove-runners
- https://docs.github.com/en/actions/how-tos/manage-workflow-runs/re-run-workflows-and-jobs

Not performed: no live runner, label, variable, workflow, DELETE, Stop Now, registration or request
operation; no build or test run; no private-repo write.
