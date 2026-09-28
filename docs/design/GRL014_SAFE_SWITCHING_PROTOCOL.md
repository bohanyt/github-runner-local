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

**Revision 2 (correction pass for CT packet Issue #18 `5852943229`, key `GRL-PR23-D1-D3-20260927`).**
§0 is authoritative wherever it conflicts with §1–§22.

- Revision 1 is PR #23 head `5e5ce953272b3c1a194d0d16ab8813b539690a89`.
- In revision 1, the label fence, the `GRL_ADMISSION` variable, the timing constants and the
  frozen-set stop proof (§7E, §9–§12, §15) are **superseded** by §0.
- They are kept below only as the record of the rejected reasoning. The research, the source
  appendix and the persistent-registration objective are retained.

---

## 0. Correction pass — D1/D2/D3 (authoritative)

### 0.1 Outcome

**Outcome A: an implementation-ready recommendation with explicitly unavailable operations.**

Two mechanisms replace the revision-1 fences. Neither relies on GitHub label/variable propagation
or on API staleness bounds.

1. **Runner-local job gate.** It is enforced before any default-condition workflow step, for every
   workflow SHA. It uses the runner's supported job-started / job-completed hooks and product-owned
   local gate state.
2. **Serialized ownership log.** It is a Git reference updated only by fast-forward
   compare-and-swap. It decides which machine may make its gate pass-capable.

Both registrations stay persistent and both keep `grl-exec`, so the D30 wording is preserved.

**No GitHub-side label, variable or workflow change is required for safety.**

Operations that stay unavailable are listed in §0.9.

### 0.2 Resolution table

| Gap | Old design failure (trace §0.4–0.6) | Corrected rule | Primitive / evidence | Deterministic test | Remaining |
|---|---|---|---|---|---|
| **D1** ownership | Two clients read `H=∅` and both add a label. X's T4 rollback re-adds its label after Y acquired. T9 abort after transfer. | Ownership lives only in a CAS'd Git ref log. A machine may set its gate `ACTIVE` only after its own `ACQUIRED(e)` commit lands. X publishes `RELEASED(e)` only after its gate is `INACTIVE` and all passed jobs are done. Abort is local and only before `RELEASED`. There is no label-based rollback and no remote takeover. | `PATCH /git/refs` with `force=false` (documented fast-forward-only), `POST /git/refs` (create-if-absent). Model: 63,126 states, no dual pass-capable gates. | D1-T (§0.4) | **OD-G:** a CAS repo permission. Forced takeover is unavailable (needs OD-E + lease). |
| **D2** stop proof | An assignment committed before the fence is delivered after two quiet reads, then killed. | Stop permission comes from a **local proof**: gate `INACTIVE` written, every job intent resolved, every passed job has a done record (or no `Runner.Worker` exists). Stop is **unavailable** in `ACTIVE`/`DRAINING`, apart from the existing warned emergency Stop Now (D31). GitHub observation is used for liveness only. | The job-started hook runs as a pre-job step before workflow steps (v2.337.0 source). Intent-first file handshake. Model: 42,434 states, no kill of a passed job; negative control fails as expected. | D2-T (§0.5) | `always()` steps still run after a hook refusal (inventory §0.6). Trusted-code assumption A3. |
| **D3** rerun affinity | Rerun of `execute` after X stopped routes to Y and runs with X's admission. Historical-SHA reruns use old workflow code. | The hook refuses unless the workflow SHA is allowlisted, the gate is pass-capable, and (for non-`admit` jobs) the run was admitted **on this machine in this gate epoch**. This is enforced before any default-condition step, for any workflow SHA. | Runner-level hook, independent of workflow content. All five private-main commits inspected. Model: 12/12 cases. | D3-T (§0.6) | A future workflow that adds `always()`/`failure()` steps must pass a new lint rule. |

### 0.3 Corrected architecture

#### Local gate (per machine; product-owned; outside `_work`)

State file `grl-gate/state.json`, written with an atomic replace:

```
{ repo, runnerId, runnerName, ownershipEpoch, gateEpoch,
  mode: ACTIVE | DRAINING | INACTIVE, allowedWorkflowShas[] }
```

- Ledger: `grl-gate/ledger/<gateEpoch>/<runId>`, created only by an `admit` pass.
- Job records: `grl-gate/jobs/<runId>-<attempt>-<job>-<nonce>.{intent,pass,refuse,done}`.
- A missing or unreadable state means `INACTIVE`, so the gate fails closed.

The product configures the runner root `.env` with:
- `ACTIONS_RUNNER_HOOK_JOB_STARTED=<root>\grl-gate\job-started.js`
- `ACTIONS_RUNNER_HOOK_JOB_COMPLETED=<root>\grl-gate\job-completed.js`

Using `.env` is the documented configuration method, and changes need a runner restart (FACT, docs).
v2.337.0 runs `.js` hooks with the runner's bundled Node (FACT, `HostContext.cs` L812–834). This avoids
PowerShell execution-policy dependence.

#### Hook decision (`job-started.js`)

It runs intent-first:

1. Write `*.intent`, then flush.
2. Read `state.json`.
3. Decide, following the rules below.
4. Write `*.pass` or `*.refuse`.
5. Exit 0 on pass, non-zero on refuse.

A missing hook file also fails the job, because the runner throws `FileNotFoundException`.

Decision rules, applied in order:

| Condition (from default env `GITHUB_REPOSITORY`, `GITHUB_SHA`, `GITHUB_WORKFLOW_SHA`, `GITHUB_RUN_ID`, `GITHUB_RUN_ATTEMPT`, `GITHUB_JOB`) | Decision |
|---|---|
| Repo ≠ state repo, or the SHA is not in `allowedWorkflowShas`, or `GITHUB_SHA ≠ GITHUB_WORKFLOW_SHA` | refuse |
| `mode = INACTIVE` | refuse |
| `GITHUB_JOB = admit` and `mode = ACTIVE` | pass; create ledger entry `(gateEpoch, runId)` **before** writing `.pass` |
| `GITHUB_JOB = admit` and `mode = DRAINING` | refuse (local admission close) |
| `GITHUB_JOB ∈ {execute, report, verdict}` | pass iff ledger `(gateEpoch, runId)` exists; else refuse |
| Any other job name | refuse |

`job-completed.js` writes `*.done` for passed jobs. It is registered by the runner as a post-job
step with `always()` (FACT, `JobExtension.cs` L570–580).

#### Ownership log (cross-machine; OD-G)

A single ref `refs/heads/grl-control` lives in a dedicated private control repository. Recommended
repository: `github-runner-local-control`. The ref must live **outside** the execution repo, so the
product never needs Contents write where workflows live.

- Each record is an empty-tree commit whose message is a JSON line:
  `{ v:1, epoch, holder:{name,runnerId}, state: ACQUIRED | RELEASED, at }`.
- Its parent is the head that was observed.
- Mutation happens only through `PATCH /git/refs/heads/grl-control` with `force=false`. Docs: "make sure
  the update is a fast-forward update … not overwriting work". The first record uses `POST /git/refs`,
  which fails if the ref exists.
- A stale client, or any delayed or duplicated request, whose parent is no longer the head **cannot**
  land, because the update is not a fast-forward.

#### Ordering rules

- **O1 — Startup.** Before starting `run.cmd`, the product writes `mode=INACTIVE`. It may set `ACTIVE`,
  with a fresh `gateEpoch` and an empty ledger, only after a **fresh** read shows
  `ACQUIRED(epoch, holder=self)` as the ref head. If the ref is unreadable, the gate stays `INACTIVE`.
- **O2 — Release (X).**
  1. `ACTIVE → DRAINING`: admit is refused locally.
  2. A liveness wait for ledger runs. This is advisory only.
  3. `DRAINING → INACTIVE`.
  4. Resolve all intents.
  5. Require every `.pass` to have a `.done`, or require that no `Runner.Worker` exists.
  6. Only then CAS `RELEASED(e)`.
- **O3 — Acquire (Y).** Read the head. Require `RELEASED(e)`. CAS `ACQUIRED(e+1, Y)` with that head as
  parent. Only after it lands (verified by re-read on an uncertain response) does Y apply O1 to become
  `ACTIVE`.
- **O4 — Abort / rollback ownership.**
  - X may return `DRAINING/INACTIVE → ACTIVE` only while the ref head is still `ACQUIRED(e, X)`, i.e.
    before X's `RELEASED(e)` lands. This is purely local because nobody else can acquire.
  - After `RELEASED(e)`, X can become `ACTIVE` again only by winning a new O3 CAS.
  - There is no label rollback, and admission is never reopened for a machine that lost the CAS.
- **O5 — Pause (same machine, no handoff).** Steps O2.1–O2.5, then stop. Ownership stays `ACQUIRED(X)`.
  Resume uses O1 with a new `gateEpoch`, so earlier ledger entries no longer count.

**I2 (restated, not weakened; now execution-level).** At every instant, at most one machine's gate is
pass-capable (`ACTIVE`/`DRAINING`), and at most one machine has passed-but-unfinished jobs. The
argument:

- X's pass-capable interval ends (O2.3) and its passed jobs finish (O2.5) **before** `RELEASED(e)`
  exists.
- Y's pass-capable interval starts only **after** its `ACQUIRED(e+1)` landed, and that CAS requires
  `RELEASED(e)` as parent.
- The CAS admits exactly one successor per parent.

**Routing.** Both runners may keep `grl-exec`. A job routed to an online machine whose gate is not
pass-capable is **refused** before any default-condition step. That costs liveness (the request fails and
must be re-posted), not safety. Operational rule: keep the inactive runner stopped. Stopping it is now
provably safe under O2/O5.

### 0.4 D1 — ownership / concurrent acquire / rollback

**Old-design counterexamples (ordered):**

1. **Two readers.** `Y:readH(∅)` → `X:readH(∅)` → `Y:addLabel` → `X:addLabel` gives `H={X,Y}`. The
   disposable model reproduces this in 4 steps.
2. **Stale rollback.**
   - X: T3 fence → T4 verifying (the old doc allowed `H={Y}`).
   - Y: T6 → T7 adds label → `H={Y}` → T8 opens.
   - X: T4 detects a job bound to X → ROLLBACK_FENCE re-adds label → `H={X,Y}`.
3. **Abort after transfer.** X is released (label removed) → Y acquires → the owner clicks T9 Abort on X →
   X re-adds its label and sets `V=open` → two holders.

**Corrected rule:** O1–O4.

- The only cross-machine write is a fast-forward CAS on one ref.
- Acquire requires `RELEASED` as the exact parent.
- Local `ACTIVE` requires the machine's own `ACQUIRED` to be the fresh head.
- Rollback and abort are local and allowed only while still owner.

Stale-client exclusion:
- A stale CAS is not a fast-forward, so it is rejected.
- A stale **local** belief cannot pass jobs after a restart, because O1 requires a fresh read.
- A long-running `ACTIVE` X cannot be superseded at all, because no takeover exists.

**Evidence and assumptions:**
- FACT (docs): `force=false` guarantees fast-forward-only; 409/422 on conflict.
- **A5 (INFERENCE):** GitHub applies ref updates atomically per ref, which is standard git ref-transaction
  semantics.
- **A6:** each machine's product is the only writer of its gate state (single-instance product).
- The App needs Contents read/write on the control repo only (**OD-G**; INFERENCE on the permission name).

**D1-T (deterministic):**
- A linearizable fake ref with separate send/land steps, so requests can land late or duplicated.
- Controllers X and Y run release, acquire, re-acquire and abort.
- The run enumerates all interleavings and asserts that at no step are two gates pass-capable.
- Also assert:
  - X's abort is refused once `RELEASED` landed;
  - a lost CAS response is resolved by re-read;
  - a delayed stale CAS never lands;
  - an unreadable ref keeps the gate `INACTIVE`.
- Negative control: the revision-1 label algorithm must violate I2.

**Unsupported, so unavailable:** remote release / forced takeover. It would need an owner-attested
"X is gone" record **plus** a gate lease with an explicit bounded-clock-drift assumption. That is OD-E and
a separate design.

### 0.5 D2 — stop permission without GitHub timing bounds

**Old-design counterexample (ordered, no layer malfunction needed):**

1. The server commits an assignment of job J to X, under labels still present.
2. The product removes the label.
3. Quiet read 1 (idle).
4. Quiet read 2 (idle).
5. J is delivered and starts.
6. The product kills the tree, and J is terminated.

Delivery and observation delay have no documented bound, so no timer makes steps 3–4 prove anything.
The claim "three independent failures must coincide" is **withdrawn**: the counterexample needs no
failure at all, and API mechanisms are not independent failure sources.

**The four concerns, separated:**

| Concern | Revision 2 treatment |
|---|---|
| Routing eligibility change | Not used for safety. Labels and variables are untouched. |
| Assignments already committed | Every job that reaches X, however late, must pass X's local hook before any default-condition step. After `INACTIVE` is written, none can pass (except the handshake race, which is closed by intent-first). |
| API observation freshness/completeness | Liveness only: when to move `DRAINING → INACTIVE`. Early or late observation never makes the stop unsafe; it can only cause a downstream job to be **refused** (the request fails visibly and is re-posted). |
| Permission to terminate the owned process | **Local proof only** (O2.3–O2.5): gate `INACTIVE` is durable; no intent is unresolved; every `.pass` has `.done`, or the owned process tree contains no `Runner.Worker`. |

Why the proof covers late assignments:
- Any job delivered after the proof either writes its intent after `INACTIVE` (so it reads `INACTIVE` and
  refuses), or is killed before or during the hook. In either case no workflow step with a default
  condition has run.
- The documented 60-second pickup rule (FACT) additionally means an assignment X never picked up is
  re-queued for another runner. That runner's own gate decides.

**Crash cases.** Every hook runs inside a `Runner.Worker` process.
- An unresolved `.intent`, or a `.pass` without `.done`, **with no `Runner.Worker` in the owned tree**, is
  recorded as `GATE_ANOMALY`. It permits the stop, because no hook or step can be executing.
- With any `Runner.Worker` present, the product waits. There is no timer-based override.

**Refined I1:** no automatic stop terminates a job that has **passed** the local gate. A job that could
not pass may be refused or terminated; either way it ends failed without executing a default-condition
step.

**Evidence and assumptions:**
- FACT: `JobExtension.cs` L301–311 inserts the job-started hook into the pre-job steps, and L582–584
  places pre-job steps before job steps.
- FACT (docs): "If there is any other exit code, the job will not run and will be marked as failed."
- FACT (source): the steps are processed in one queue, so `always()` job steps are still evaluated after a
  failed pre-job step. See §0.6 for why this is harmless for every existing workflow blob.
- **A1 (INFERENCE, witness):** the Worker inherits the hook variables from the `.env` the listener loads
  at startup (`Program.cs` `LoadAndSetEnv`).
- **A3:** trusted-code mode (D08/D14). Jobs run as the same OS user and could tamper with gate files; the
  gate is a race/affinity control, not a security boundary.
- **A4:** local NTFS ordering. An atomic replace plus flush of the state file, and intent files created
  and flushed before the state read.

**D2-T (deterministic):**
- The product sequence is: write `INACTIVE` → scan → stop. Up to 3 jobs, each running the hook sequence
  intent → read → decide → run → done, with arbitrary interleaving and a crash at any step.
- Assert that the stop never happens while any job is passed-and-not-done.
- Assert that a job arriving after the proof is refused.
- Assert that a worker crash (pass without done, no worker) is recorded as an anomaly and permits the stop.
- Negative controls:
  - a read-first hook must fail;
  - the revision-1 "fence + two quiet reads + delayed delivery" model must fail.

**Unavailable:** any automatic stop while `ACTIVE` or `DRAINING`. The existing warned emergency Stop Now
remains (D31) and records `RECOVERY_REQUIRED`.

### 0.6 D3 — late and historical rerun affinity

**Old-design counterexamples (ordered):**

1. **Rerun-failed after X stopped.**
   - Request R is admitted on X; `execute` fails.
   - The switch completes: X is stopped and Y is active with `grl-exec`.
   - The owner re-runs failed jobs: `execute`, `report` and `verdict` are routed to Y.
   - Y executes the profile using X's admission evidence (disk/elevation/capabilities measured on X).
2. **Rerun-one-job during activation.** Y has just added its label (T7). A rerun of R's `execute` is
   assigned to Y before T8. Same violation.
3. **Historical SHA.** A rerun of a run created at private commit `37b0f13` (temporary fault-injected
   overlay) re-executes that commit's workflow and actions. A new guard deployed on main does not apply,
   because reruns use the original `GITHUB_SHA` (FACT, docs).

**Corrected rule (hook table, §0.3):**
- Non-`admit` jobs pass only if **this machine** created the ledger entry for this run **in its current
  gate epoch**.
- Every job is refused unless its workflow SHA is on the product's reviewed allowlist. Initially the
  allowlist is only `2c8da8834e785ba012001b9427bd68380401bec7`.

Applied to each rerun timing:

| Timing | Result |
|---|---|
| After X stopped | Routed to Y; refused (no ledger entry on Y). |
| During activation | Refused (Y's new epoch ledger is empty). |
| After the switch | Refused on Y. On X after a later re-acquire, refused too (new epoch). |
| Historical SHA | Refused on any machine, because the hook is runner-level and applies regardless of workflow content. |

A full "re-run all jobs" re-runs `admit` on the current active machine. That machine re-admits (or
refuses) with **its own** checks and ledger, so affinity holds.

**`always()` inventory (FACT, blobs inspected).** Private main history is `890829e` (no workflow),
`3ca0449`, `dfb1dfd`, `2c8da88`, with blob `71e0271ee9c1146ebcdc2ac9d982b32c1a773b5d`, and `37b0f13`,
with blob `003e2825a9e5844d2237998baebfba24d4df5905`.

- Each has exactly one step with `if: always()`: `actions/upload-artifact` of `grl-outcome` in
  `execute`.
- After a hook refusal the profile step is skipped (default condition), so the upload finds no files and
  only warns (`if-no-files-found: warn`).
- The second upload requires `execution_status == 'BLOCKED'` from the skipped step, so it is false.
- Therefore no profile or target code runs after a refusal in any existing blob. Reruns of `37b0f13` are
  refused by the SHA allowlist in any case.

**New lint rule (template):** reject any step condition containing `always()`, `failure()` or
`cancelled()` except that exact upload step. This keeps the property for future SHAs, which still also
require allowlisting.

The same rule must reject any referenced action that declares a `pre` step. An action's `pre-if`
defaults to `always()`, and pre-steps are queued after the hook but are not skipped by its failure.

INFERENCE (verify at implementation against the pinned SHAs):
- the pinned `actions/checkout@11bd7190…`, `actions/upload-artifact@ea165f8d…` and
  `actions/download-artifact@d3f86a10…` declare no `pre`;
- the local `grl-*` actions declare only `main`;
- `post` steps are registered only when their main step ran.

**D3-T (deterministic):**
- A table-driven run of hook decisions over `{machine, gateEpoch, mode, job, runId, sha}` for: rerun-failed
  after stop; rerun-one-job during activation and after the switch; rerun after the old owner re-acquires;
  historical SHAs `3ca0449` and `37b0f13`; a fresh request on the new owner.
- Plus a lint test for the `always()` inventory.

**Remaining:**
- Wrong-host downstream work is now **refused, not executed**. The affected request fails and must be
  re-posted.
- Each future private workflow deploy requires an explicit allowlist update in the product. This is
  fail-closed: new SHAs are refused until allowlisted.

### 0.7 What revision 2 drops from revision 1

These are not required for safety:
- the `GRL_ADMISSION` variable (so OD-B is withdrawn);
- the label fence and label rollback;
- label-less enrollment (the D30 wording is kept);
- the timing constants as a safety argument;
- the "frozen-set" stop.

The following are kept:
- all source and doc research (§5, §6, §22);
- the rejection of DELETE, `busy=false` and plain Stop Now (§8);
- persistent registrations;
- the failure-matrix discipline (§13), where the "never automatically" column still applies.

### 0.8 Model execution disclosure

A disposable offline model was actually executed. It is kept local and is not committed.

- File SHA-256: `35DD5C887BAC595369A173E12993943ED944516ABB25C3A5C75D5291048F21B0`.
- Run with Node v24.14.1 on the design worker, exit code 0.

Results (DFS over all interleavings of bounded programs):

| Check | Expected | Result |
|---|---|---|
| D1 revision-1 label algorithm | violation expected | Violated (two holders) after 7 states. |
| D1 CAS log + gate, with delayed/duplicated deliveries | no violation | None in 63,126 states. |
| D2 revision-1 fence + two quiet reads + unbounded delivery | violation expected | Violated after 14 states. |
| D2 intent-first handshake | no violation | None in 42,434 states. |
| D2 read-first handshake | violation expected | Violated after 48 states. |
| D3 decision table | as expected | 12/12 cases as expected. |

This is design-level evidence only. It is **not** a product test and not `LOCAL_CHECKED` for any
implementation.

### 0.9 Minimal source-only scope for the Sol worker, and what stays unavailable

**Implementable now (no new permission; single machine):**

- **S1 — Gate.**
  - `job-started.js` / `job-completed.js` with the §0.3 rules and intent-first handshake.
  - Gate state, ledger and job-record formats, with atomic file helpers.
  - The product writes the runner root `.env` hook variables.
  - The SHA allowlist is product configuration.
- **S2 — Local lifecycle.**
  - O1 startup (`INACTIVE` default).
  - O2 Release steps 1–5, and O5 Pause/Resume with new gate epochs.
  - Enumeration of `Runner.Worker` in the owned process tree.
  - A proof-gated stop that replaces "Stop Now" for planned pause.
  - Emergency Stop Now stays warned and records `RECOVERY_REQUIRED`.
- **S5 — E template lint.**
  - The `always()`/`failure()`/`cancelled()` inventory rule and its tests.
  - No workflow behaviour change and no private deploy.
- **S6 — Tests.**
  - D1-T, D2-T and D3-T as unit tests with fakes.
  - Hook-script tests run with Node.

**Single-machine mode rule (without OD-G).** The gate may become `ACTIVE` without an ownership log only
when the fresh runner list for the repo contains **exactly one** registration and it matches the
persisted ID and name. A second registration puts the gate in `INACTIVE` + `SWITCH_BLOCKED`. Enrolling a
second machine therefore requires OD-G first, so I2 is never left to an unenforced assumption.

This check is a snapshot. It is sufficient only because registering a second runner is itself a
separately authorized owner action (D30/OD-F). It is not a substitute for the CAS log.

**Implementable as source behind a disabled capability (needs OD-G before any live use):**

- **S3 — Ownership-log client** (Git refs/commits API, fake-tested).
- **S4 — Cross-machine Release step 6 + Acquire (O3).**

**Unavailable:**

| Operation | Why unavailable | What would enable it |
|---|---|---|
| Automatic stop in `ACTIVE`/`DRAINING` | No proof exists in those states | Nothing. It is replaced by Release/Pause. |
| Remote release / forced takeover | See §0.4 | OD-E plus a lease design. |
| Scheduled keep-alive | — | OD-D. |
| Personal enrollment | — | D30 + OD-F. |
| Live witnesses (§18, revised) | — | OD-F. |

Revised live witnesses:
- **W1′:** hook refusal on ID 3 for a non-allowlisted SHA or a non-ledger downstream.
- **W2′:** planned Pause with proof-gated stop, then Resume.
- **W3′:** one-machine Release → Acquire via the ownership log (needs OD-G).
- **W4′:** mid-request drain, where downstream work is allowed while `DRAINING`.

All use js-smoke only.

---

## 1. Executive summary (revision 2)

**Recommendation (PROPOSED, revision 2).** Enforce one-active-at-a-time **on the runner machines
themselves**:

- a supported job-started hook refuses every job unless this machine's gate is pass-capable, the workflow
  SHA is allowlisted, and (for downstream jobs) the run was admitted here in the current gate epoch;
- cross-machine ownership moves only through a fast-forward-CAS Git ref log;
- stop is permitted only by a local proof that no passed job is unfinished.

Details and evidence are in §0.

**Why it is safe, and under which assumptions.**
- I1 (no passed job killed), I2 (one pass-capable machine) and I3 (affinity) rest on local ordering plus
  one atomic CAS.
- They do not rest on GitHub routing, propagation or API freshness.
- The assumptions are A1 (hook environment inheritance, witness needed), A3 (trusted-code mode),
  A4 (local file ordering), A5 (atomic ref update) and A6 (single product writer per machine).

**Two persistent registrations survive**, and both keep `grl-exec`, as D30 is worded.

**Owner decisions** are listed in §20: OD-A (revised), OD-C, OD-D, OD-E, OD-F and new OD-G. OD-B is
withdrawn.

*(The revision-1 summary is superseded. Its "three independent failures" claim is withdrawn; see §0.5.)*

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

PROPOSED invariants for any switching protocol. These are the revision-2 wording; see §0 for how each is
enforced.

- **I1 No interruption.** No automatic operation terminates a runner process while any job that has
  **passed the local gate** is unfinished. A job that cannot pass the gate may be refused or terminated;
  either way it ends failed without executing a default-condition workflow step.

  Revision 1 phrased this as "while any job is assigned". That is not provable without a documented
  delivery bound (§0.5).
- **I2 Single active (execution-level).** At every instant, at most one machine's gate is pass-capable,
  and at most one machine has passed-but-unfinished jobs. It is enforced by the CAS ownership log plus
  local ordering (§0.3).

  Revision 1 phrased this as "at most one registration holds `grl-exec`". That was not enforceable with
  non-atomic label writes (§0.4). Both registrations may now hold `grl-exec`; a job routed to a
  non-pass-capable machine is refused (a liveness cost only).
- **I3 Request affinity.** Every non-`admit` job executes only on the machine that admitted its run, in
  the same gate epoch. Otherwise it is refused before any default-condition step. This holds for reruns
  and for historical workflow SHAs (§0.6).
- **I4 Truthful uncertainty.** Any unreadable, ambiguous or contradictory observation yields a pending or
  blocked state. Nothing is ever automatically stopped, deleted, relabelled-to-active or reopened on
  uncertainty.
- **I5 Exact identity.** Every mutation targets repo full name + numeric runner ID + exact name, re-verified
  by a fresh read immediately before the mutation. There is no name-only adoption and no historical ID 2.

"Active" is defined (revision 2) as **being the holder of the latest `ACQUIRED` record in the ownership
log, with a pass-capable local gate**. Holding `grl-exec` or being online alone is not active.

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

> **SUPERSEDED by §0 (revision 2).** Kept only as a record of revision 1. Do not implement this section.

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

> **SUPERSEDED by §0 (revision 2).** Kept only as a record of revision 1. Do not implement this section.

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

> **SUPERSEDED by §0 (revision 2).** Kept only as a record of revision 1. Do not implement this section.

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

> **SUPERSEDED by §0.3 O2/O3.** Office→personal is: Release on office (O2) → Acquire on personal (O3). Remote release (§11.3 below) is unavailable (§0.4).

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

> **SUPERSEDED by §0.3.** The procedure is symmetric: Release (O2) then Acquire (O3). Same-machine Pause/Resume is O5.

This is symmetric. Release on personal (T1–T5), then acquire on office (T6–T8) using the **existing ID 3
registration** (resume path, no configure). Rollback to office after a failed personal activation is just
"Acquire on office". Both paths reuse the planned-pause resume proven in GRL-015.

**One-machine full cycle:** release office → acquire office. This exercises every transition without
enrolling personal and is the basis of live witness W3 (§18).

---

## 13. Failure / recovery matrix

> **Revision 2:** rows that act on labels, `GRL_ADMISSION` or T-states are superseded by §0. The "Never" column still holds. Revision-2 additions:
>
> - a job reaching a non-pass-capable gate is refused (the request is re-posted);
> - an unreadable ownership ref keeps the gate `INACTIVE`;
> - a lost CAS response is resolved by re-read;
> - pass-without-done with no `Runner.Worker` is recorded as an anomaly and permits the stop;
> - a missing or corrupt gate state means `INACTIVE`.

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

**Both persistent registrations survive (revision 2).** Office ID 3 and a future personal ID each keep
their own credentials on their own laptop, and **both keep `grl-exec`**, as D30 is worded. No credential
is copied. A switch uses:
- local gate transitions;
- one or two CAS commits on the ownership log;
- a local, proof-gated stop and start.

No DELETE, no re-registration and no label change are involved.

Refinements the owner must see:

1. **OWNER DECISION REQUIRED — OD-A (revised).** "Active" means the ownership-log holder with a
   pass-capable gate, not the holder of `grl-exec`.

   Consequence: while both runners are online, GitHub may route a job to the inactive one, which refuses
   it. The request fails and must be re-posted. The operating rule is therefore to keep the inactive
   runner stopped, which is now provably safe to do.

   The revision-1 proposals are withdrawn: "`grl-exec` only on the active runner" and "enroll personal
   without `grl-exec`".
2. **14-day auto-removal (FACT).** An inactive laptop that never connects for more than 14 days loses its
   registration. A start of the inactive runner with its gate `INACTIVE` is safe, but jobs routed to it
   while it is online are refused.

   PROPOSED: the owner runs such a keep-alive start at least every ~10 days at a quiet time. This is a
   proposal, not approved automation (OD-D). Otherwise, accept owner-confirmed re-registration.
3. **30-day update rule (FACT)** applies to both laptops. It interacts with the pinned-version gating
   (D22) and is not changed here.
4. **No other workflow may target `[self-hosted]` alone** in the execution repo. The gate would refuse
   such jobs anyway, because their workflow SHA or job name is not allowlisted.

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

> **SUPERSEDED by §0.9.** M1 (the GRL_ADMISSION clause) and the label APIs of M2 are not required. M4–M8 are replaced by S1–S6.

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

> **Revision 2:** the focused specifications D1-T, D2-T and D3-T in §0.4–§0.6 take precedence. The label/variable scenarios below remain useful only as negative controls.

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

> **Revision 2:** W1–W4 below are replaced by W1′–W4′ (§0.9). W5 still requires personal-enrollment authorization. Nothing is authorized.

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

> **Revision 2:** no label or variable writes are needed. The only new remote write is the ownership ref in a dedicated control repository (OD-G, Contents on that repo only). Gate files hold repo, run, job and epoch identifiers only, with no tokens. The gate is not a security boundary against workflow code running as the same user (A3).

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

All items below remain **PENDING**. No owner approval is recorded. The owner's preference for Sol as
implementer approves none of them. Revision 2:

- **OD-A (revised).** Define "active" as ownership-log holder + pass-capable local gate. Both registrations
  keep `grl-exec`. Accept that jobs routed to an online inactive runner are refused, and keep the inactive
  runner stopped.
- **OD-B — withdrawn.** The Actions Variables permission is no longer needed; no `GRL_ADMISSION`
  variable exists.
- **OD-C.** Accept that requests arriving while no gate admits (release, pause, or an inactive-but-online
  runner) are **refused** and must be re-posted. Downstream jobs of runs admitted on the old machine
  that arrive after its gate closed are also refused.
- **OD-D.** 14-day policy: an owner-run keep-alive start of the inactive laptop every ~10 days (PROPOSED,
  not automation), or owner-confirmed re-registration after auto-removal.
- **OD-E.** Remote release / forced takeover. It is **unavailable** in revision 2 and would need a separate
  lease design with a bounded-clock-drift assumption plus an owner-attested "old machine gone" record.
- **OD-F.** Authorize the Sol source-only packet (§0.9 S1, S2, S5, S6, with S3/S4 disabled), independent
  review, and later live witnesses W1′–W4′ on office ID 3. Personal enrollment still needs its own D30
  activation.
- **OD-G (new).** Create a dedicated private control repository (suggested:
  `github-runner-local-control`) and grant the GitHub App **Contents read/write on that repository only**
  for the ownership-log ref. Without OD-G, only single-machine Pause/Resume (S1/S2) is available, and
  cross-machine switching stays unavailable.
- **Not recommended:** session-scoped DELETE/re-register model (§14).

## 21. Open questions (do not block the design; each has a witness or harness hook)

Revision 2. Items 1–3 and 7 from revision 1 are no longer safety-relevant: vars timing, label
linearizability, list staleness and DELETE semantics.

1. **A1 witness.** Does the Worker see `.env` hook variables, and does a refusing `.js` hook fail the job
   before the first default-condition step on the pinned v2.337.0 Windows runner? (Windows local proof;
   later W1′.)
2. **A5.** Is the GitHub ref fast-forward update atomic under concurrent PATCH? It is documented as
   non-overwriting, and D1-T fakes it. A later live witness can issue two concurrent CAS updates against
   a scratch ref.
3. What is the exact App permission name for the Git refs/commits API on the control repo? Verify at
   implementation.
4. Does "re-run failed jobs" re-run dependents? This is now irrelevant to safety, because every job is
   gated.
5. How does the owner want requests refused during a switch to be surfaced? No ACK is posted when the
   hook refuses; only a failed run is visible.

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

Revision 2 additional sources:

- actions/runner `v2.337.0` (`397b032c…`):
  - `src/Runner.Worker/JobExtension.cs`: L297–311 (actions prepared, then the job-started hook is added to
    the pre-job steps with `always()`); L570–580 (the job-completed hook is a post-job step with
    `always()`); L582–584 (pre-job steps come before job steps).
  - `src/Runner.Worker/JobHookProvider.cs` L38–93: a missing file throws; the hook runs as a script
    step.
  - `src/Runner.Common/HostContext.cs` L812–834: `.js` hooks run on the runner's bundled Node; `.ps1`
    uses pwsh or powershell.
  - `src/Runner.Worker/StepsRunner.cs` L57–204: one step queue with per-step condition evaluation.
  - `src/Runner.Listener/Program.cs` `LoadAndSetEnv`: the root `.env` is loaded.
- Private execution repo history: `890829e`, `3ca0449`, `dfb1dfd`, `37b0f13`, `2c8da88`. Workflow blobs are
  `71e0271ee9c1146ebcdc2ac9d982b32c1a773b5d` (×3) and `003e2825a9e5844d2237998baebfba24d4df5905`
  (fault overlay). Each has one `always()` step: `upload-artifact` in `execute`.
- Docs (fetched 2026-09-27):
  - https://docs.github.com/en/actions/how-tos/manage-runners/self-hosted-runners/run-scripts — "If
    there is any other exit code, the job will not run and will be marked as failed"; `.env`
    configuration.
  - https://docs.github.com/en/rest/git/refs — `force` "make sure the update is a fast-forward update …
    not overwriting work"; 409/422.
  - https://docs.github.com/en/actions/reference/runners/self-hosted-runners — "If the runner doesn't
    pick up the assigned job within 60 seconds, the job is re-queued so that a new runner can accept it."
- Disposable model: SHA-256 `35DD5C887BAC595369A173E12993943ED944516ABB25C3A5C75D5291048F21B0`. Executed
  with Node v24.14.1; results in §0.8. It is not committed.

Not performed (both revisions):
- no live runner, label, variable, workflow, DELETE, Stop Now, registration, request or App operation;
- no product build or test run;
- no private-repo write.
