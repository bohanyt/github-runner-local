# G1 checkout-SHA-mismatch witness (acceptance-only, FAULT_INJECTED)

Authority: Issue #15 correction packets `5841236994` §3/§5 and `5841514277` (W-1 recovery). This runbook and `tools/g1-witness/overlay.mjs` are a **review-gated test harness**, not product behavior. Nothing here runs until the independent delta review and the separate CT merge/resume gate have both passed. The normal template workflow is never changed by this tooling. The public repository gains no active workflow.

## 1. What is proved and how it must be labelled

Under reviewed semantics, admission only admits a 40-hex commit that GitHub `compare` reports is contained in an allowed branch. The pinned `actions/checkout` then checks out exactly that commit, so `CHECKOUT_SHA_MISMATCH` has no natural live trigger. The witness therefore injects one controlled fault, and only for one predeclared request:

- after a genuine normal admission of requested commit **R**, the execute job's `grl-target` checkout uses a different, fixed, harmless same-repo commit **O**;
- the unchanged `grl-run-profile` observes the real `git rev-parse HEAD` = O, which is not R, runs **zero** profile processes and emits the reviewed E-B1 refusal;
- the unchanged `grl-report` and `grl-verdict` then publish `grl-exec-refusal v1`, a failing `grl/js-smoke` status on R, and a failing verdict.

Report this as **FAULT_INJECTED**. It proves defensive refusal and reporting on the real office runner. It does not prove that ordinary pinned checkout can naturally return a wrong SHA. G1 still needs the separate, unmodified positive run.

## 2. Exact overlay (the only permitted delta)

`tools/g1-witness/overlay.mjs` accepts only the reviewed workflow blob `71e0271ee9c1146ebcdc2ac9d982b32c1a773b5d` (`.github/workflows/grl-dispatch.yml`). It changes exactly these lines:

```diff
       github.event.comment.author_association == 'OWNER' &&
-      startsWith(github.event.comment.body, '<!-- grl-request v1 -->')
+      startsWith(github.event.comment.body, '<!-- grl-request v1 -->') &&
+      contains(github.event.comment.body, '"request_id":"<NONCE>"')
 ...
       - uses: actions/checkout@11bd71901bbe5b1630ceea73d27597364c9af683
         with:
-          ref: ${{ needs.admit.outputs.target_sha }}
+          ref: <O>
           path: grl-target
           persist-credentials: false
```

**What stays unchanged.** All of the following are byte-identical to the reviewed workflow:

- mailbox, PR, actor, OWNER and marker guards;
- runner labels, permissions and action pins;
- the `grl-trusted` checkout (`ref: ${{ github.sha }}`);
- the admit outputs and the report/verdict wiring;
- every action and library file.

The template linter accepts the overlay unchanged (tested). The fence is an extra `&&` condition, so it can only narrow what is admitted.

**Parameter checks.** Parameters are validated data, not commands. R and O must each be canonical lowercase 40-hex, and they must differ. `<NONCE>` must be a lowercase UUIDv4. Anything else, a drifted or CRLF-converted baseline, or an already-overlaid file is refused.

## 3. Preconditions (all must hold; otherwise stop)

The tool enforces some preconditions; the operator must check the rest. Neither kind may be skipped.

**Operator checks (not enforced by the tool):**

1. The CT resume gate is open, naming the merged corrected template. A fresh operator claim is posted on Issue #1.
2. Private `main` is already fast-forwarded to the corrected reviewed tree, and the **normal positive run is complete** (ACK, execute, report/status, verdict PASS) on commit **X**.
3. `grl-office` (runner id 2) is online, idle (`busy=false`), and the only matching runner. There are no queued or in-progress runs in the private repo. The tool cannot lock out other actors, so nobody else may post mailbox requests during the window, and automatic retries are never used.
4. The repo is private with default branch `main`: `gh api repos/bohanyt/github-runner-local-exec --jq '.private, .default_branch'` → `true`, `main`.
5. The four Actions variables are unchanged, and R and O are the harmless commits chosen in §4.

**Tool-enforced on every command** (refused before any write):

- The local clone is on branch `main`.
- Every fetch URL and every push URL of `origin` is exactly `https://github.com/bohanyt/github-runner-local-exec(.git)`. Git expands `insteadOf`/`pushInsteadOf` first, so a separate `pushurl` pointing elsewhere is refused.
- The clone does not convert line endings (`core.autocrlf` is unset or `false`).
- A fresh `git ls-remote --symref origin HEAD` reports `refs/heads/main`.
- Remote state always comes from a fresh `git ls-remote origin refs/heads/main`, never from a tracking ref such as `origin/main`. An unreachable remote is `REMOTE_UNAVAILABLE`, which means pending, never normal.

Pushing a workflow file needs a credential with `workflow` scope. The operator used Git Credential Manager for the bootstrap: set it in this clone's local config only. No new credentials, and no global auth or environment resets.

## 4. Identities and checkpoint (recorded before dispatch)

| Item | Choice |
| --- | --- |
| R (requested) | A new **empty commit** on `main` after X, "G1 witness requested-identity anchor" (same tree as X). Its failing status can never overwrite the positive status on X. |
| O (observed) | X, the corrected bootstrap commit: harmless reviewed template content, already in `main` history. |
| NONCE | A fresh UUIDv4, also used as the witness request's `request_id`. |
| P (pre-overlay) | `main` after R is pushed. `plan` requires local HEAD == the actual remote `main`, and its workflow blob must be `71e0271e…`. |

`plan` writes a non-secret checkpoint to `<CLONE>/.git/grl-g1-witness-plan.json` (inside the Git directory, so it is never committed). It records:

- schema, repository, branch and remote;
- P and P's tree;
- baseline and overlay blobs;
- R, O and NONCE;
- later, Y, Z and the fresh remote confirmations.

Every later command re-validates the checkpoint against Git objects and the supplied R/O/NONCE:

- If the plan is missing, the result is `PLAN_MISSING`.
- If the file is not valid JSON or does not match the recorded Git objects, the result is `PLAN_CORRUPT`.
- If the supplied parameters differ, the result is `PLAN_MISMATCH`.

In every case nothing is mutated. Keep a copy of the `plan` output in the local evidence notes.

## 5. Procedure

Run everything from the local private clone. `<CLONE>` is its path and `<TOOL>` is `tools/g1-witness/overlay.mjs` from the merged public repo; both are local placeholders, not published paths. `ARGS` means `--requested $R --observed $O --request-id $NONCE`. Every mutating command is **idempotent**: after an interruption, crash, network error or uncertain result, rerun the same command. It converges from the recognized state or refuses without touching anything. `node <TOOL> status <CLONE> ARGS` shows the state read-only.

1. **Anchor R.**
   - Run `git commit --allow-empty -m "G1 witness requested-identity anchor"` and `git push origin HEAD:main` (normal fast-forward).
   - Record `R=$(git rev-parse HEAD)`.
   - Confirm with `git ls-remote origin refs/heads/main` that the remote shows R.
2. **Plan (checkpoint only; the workflow is untouched).**
   - Run `node <TOOL> plan <CLONE> ARGS`.
   - Record P, the baseline and overlay blobs, the expected refusal and the exact two-location delta.
3. **Isolate outcome evidence.** Proceed only while `busy=false` and no `Runner.Worker` process exists.
   - Move the workspace's `grl-outcome` folder, if present, to a dated checkpoint folder outside `_work`. The path is `<RUNNER_ROOT>\_work\github-runner-local-exec\github-runner-local-exec\grl-outcome`.
   - `run-profile` does not delete old files, so a stale positive `result.json` could otherwise ride along in the refusal artifact (tested).
   - Touch nothing else: no credentials, `.runner`, other workspaces, runner root or registration.
4. **Apply → `OVERLAY_ACTIVE_CONFIRMED`.** Run `node <TOOL> apply <CLONE> ARGS`, which:
   - atomically writes the expected overlay bytes;
   - stages and commits **only** the workflow (`TEMPORARY G1 witness overlay (FAULT_INJECTED, acceptance-only)`), giving commit Y = P + the overlay;
   - pushes `Y:refs/heads/main` without force;
   - re-reads the actual remote.

   Then run `node <TOOL> verify-overlay <CLONE> ARGS` and record Y.

   If the result is `APPLY_PUSH_PENDING` or `PUSH_UNCERTAIN`, rerun `apply`. It confirms a push that already landed, or pushes again, and never creates a second commit.
5. **Dispatch exactly one request.**
   - Build `grl-request v1` with `request_id=<NONCE>`, `target.sha=R`, profile `js-smoke` at its reviewed definition blob, timeout 5 and a 2-hour expiry.
   - Serialize it compactly (`JSON.stringify`) so the body contains `"request_id":"<NONCE>"`.
   - Validate it with the reviewed `parseRequestEnvelope`, then post it once on the mailbox.
   - If the post result is uncertain, inspect the mailbox and runs before doing anything else. Never repost blindly.
6. **Observe** (expected results):
   - **ACK:** `admitted: true`, `requested_sha=R`, `workflow_sha=Y`.
   - **Execute job:** its `grl-target` checkout log shows O; `grl-run-profile` returns `execution_status=BLOCKED`.
   - **Artifacts:** `grl-exec-refusal-<run>-<attempt>` exists.
   - **Report:** a `<!-- grl-exec-refusal v1 -->` comment with `reason_code: CHECKOUT_SHA_MISMATCH`, `profile_executed: false`, `observed_checkout_sha=O`, and `status_posted`/`comment_posted` both true. There is no `grl-result v1` for this request, and a failing `grl/js-smoke` status on R whose `target_url` is the run.
   - **Verdict job:** fails. Record run id/attempt, job ids and comment ids.
7. **Restore → `NORMAL_CONFIRMED` — always, even if step 4 or step 6 failed, timed out or never started.** An in-flight run keeps using Y's workflow, so restoring cannot alter it. Run `node <TOOL> restore <CLONE> ARGS` and rerun it until it returns `NORMAL_CONFIRMED` or a refusal that needs a human.
   - **Overlay committed as Y:** the tool writes the exact baseline bytes, commits only the workflow as Z (parent Y, tree == P's tree), and pushes `Z` without force. It then runs the full normal verification below.
   - **Overlay never committed** (HEAD still P and the remote still P): it only returns the workflow to the baseline bytes, with no commit and no push (`reason: CANCELLED_BEFORE_COMMIT`).
   - **Overlay committed but never pushed:** pushing Z fast-forwards the remote from P through Y to Z. The remote never has Y as its head, so no override becomes active.
   - **Push rejected or unknown outcome:** the result is `RESTORE_PUSH_PENDING` or `PUSH_UNCERTAIN`. That means the remote may still be overlaid and normal dispatch is **prohibited**. Rerun `restore`: if the push actually landed, it is confirmed without a new push or commit.
8. **Verify normal operation.** Run `node <TOOL> verify-normal <CLONE> ARGS`; this is also the last step of `restore`. `NORMAL_CONFIRMED` requires ALL of:
   - the workflow is the exact baseline blob `71e0271e…` in HEAD, index and worktree;
   - no other staged, unstaged or untracked change;
   - local HEAD tree == P's tree;
   - a **fresh** `ls-remote` shows the actual remote `main` == this local HEAD.

   The confirmed remote SHA is recorded in the checkpoint. A clean local baseline with the remote still at Y is `REMOTE_STILL_OVERLAID`, and an unreachable remote is `REMOTE_UNAVAILABLE`. Neither is normal. Also confirm the four variables are unchanged and that no other workflow file exists. Only after `NORMAL_CONFIRMED` may normal mailbox requests (no nonce) resume.

### Recognized states (what a rerun does)

| Interrupted after | Local HEAD / workflow (index, worktree) | Remote | Rerun `apply` | Rerun `restore` |
| --- | --- | --- | --- | --- |
| apply wrote bytes | P / overlay (unstaged or staged) | P | commit Y, push | return to baseline, no commit (cancel) |
| apply commit | Y / overlay | P | push Y | commit Z, push (remote P→Z) |
| apply push, ack lost | Y / overlay | Y | confirm, no push | commit Z, push |
| restore wrote bytes | Y / baseline (unstaged or staged) | Y | refuse `RESTORE_IN_PROGRESS` | commit Z, push |
| restore commit | Z / baseline | Y | refuse `ALREADY_RESTORED` | push Z |
| restore push, ack lost | Z / baseline | Z | refuse `ALREADY_RESTORED` | confirm, no push/commit |

**Refused, with everything preserved as-is:**

| Refusal code | Cause |
| --- | --- |
| `UNRELATED_CHANGES` | any other tracked or untracked change (paths listed) |
| `UNKNOWN_WORKFLOW_BYTES` | e.g. a partially written file |
| `UNEXPECTED_HEAD` | a HEAD that is not exactly P, Y (only the workflow changed, to the expected overlay) or Z (tree == P) |
| `REMOTE_DRIFT` | the remote moved to anything else; it is never overwritten |
| `LINEAGE_MISMATCH` | the remote has the overlay while local HEAD is P; fetch and fast-forward first |
| `PLAN_MISSING` / `PLAN_CORRUPT` / `PLAN_MISMATCH` | bad checkpoint |
| `WRONG_DESTINATION` / `WRONG_BRANCH` / `WRONG_DEFAULT_BRANCH` / `LINE_ENDING_CONVERSION` | destination checks |

There is no global `DIRTY_TREE` relaxation. The tool never resets, cleans, stashes, force-checks-out or force-pushes. Files are replaced with a single atomic rename from a temp file in the Git directory. If a refusal leaves the overlay possibly active, report **restoration incomplete**, keep normal dispatch prohibited, and resolve it manually. Do not reset.

## 6. Abort rules

Stop without dispatching if any tool check refuses, the runner is busy or offline, a run is queued, or the plan's overlay blob changes. If the overlay may have reached the remote, still run step 7 and step 8 until `NORMAL_CONFIRMED`, or report restoration incomplete. Do not cancel an in-progress job or use Stop Now to speed things up. Never force-push, rewrite history, delete the tag or issues, or reuse the nonce. If reporting turns out incomplete, record `REPORTING_INCOMPLETE` honestly; do not rerun to manufacture a complete refusal.

## 7. Offline evidence for review

Run `node --test tools/g1-witness/overlay.test.mjs`. The fixtures are disposable local clones with a local bare `origin`, reached through the in-process `allowedUrls` test seam; the CLI accepts only the canonical private remote. The suite covers:

- **Transform checks:**
  - the pinned baseline equals the template workflow blob;
  - the exact two-location delta;
  - template lint passes on the overlay, and guard, pin, permission and label lines are identical;
  - parameter, equal-SHA, UUID-injection, drift, CRLF and double-apply refusals;
  - exact byte restoration.
- **Happy path:** plan → apply → verify-overlay → restore → verify-normal against the actual bare remote. Reruns are no-ops, and the tree == P.
- **Reviewer's W-1 point A:** an interrupted apply, with the overlay unstaged or staged, resumes forward or cancels to P with no commit.
- **Reviewer's W-1 point B:** an interrupted restore, with the baseline unstaged or staged, converges to exactly one Z.
- **Pending pushes:** an overlay committed but not pushed, and a rejected restore push, are pending. A clean local baseline with the remote still overlaid is **not** normal.
- **Lost acknowledgement:** a lost push acknowledgement for apply or restore is confirmed without a duplicate push or commit.
- **Remote unavailable:** reported as pending.
- **Destination refusals:** a wrong fetch/push URL, a non-`main` branch, a non-`main` remote default branch and `core.autocrlf=true` are all refused with no mutation.
- **Remote drift:** a foreign remote advance is refused and not overwritten; `plan` refuses a local HEAD that is not the actual remote main.
- **Preservation:** unrelated tracked and untracked edits, and partially written workflow bytes, are preserved and refused.
- **Checkpoint refusals:** missing, corrupt and mismatched checkpoints.
- **Lineage refusal:** an unrecognized HEAD lineage.
- **Status:** the `status` command is read-only.
- **Real-HEAD refusal:** a real `git` checkout at O is fed to the **unchanged** `grl-run-profile` entrypoint, which yields the exact `BLOCKED/CHECKOUT_SHA_MISMATCH` outcome with zero profile processes and no canonical result.
