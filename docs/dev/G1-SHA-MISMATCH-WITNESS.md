# G1 checkout-SHA-mismatch witness (acceptance-only, FAULT_INJECTED)

Authority: Issue #15 correction packet `5841236994` §3/§5. This runbook and `tools/g1-witness/overlay.mjs` are a **review-gated test harness**, not product behavior. Nothing here runs until the independent delta review and the separate CT merge/resume gate have both passed. The normal template workflow is never changed by this tooling. The public repository gains no active workflow.

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

1. The CT resume gate is open, naming the merged corrected template. A fresh operator claim is posted on Issue #1.
2. Private `main` is already fast-forwarded to the corrected reviewed tree, and the **normal positive run is complete** (ACK, execute, report/status, verdict PASS) on commit **X**.
3. `grl-office` (runner id 2) is online, idle (`busy=false`), and the only matching runner. There are no queued or in-progress runs in the private repo.
4. Nobody else will post mailbox requests during the window. Automatic retries are never used.
5. Local clone of the private repo exists with `core.autocrlf=false` and a clean tree. Pushing a workflow file needs a credential with `workflow` scope; the operator used Git Credential Manager for the bootstrap.

## 4. Identities (record all before dispatch)

| Item | Choice |
| --- | --- |
| R (requested) | A new **empty commit** on `main` after X, "G1 witness requested-identity anchor" (same tree as X). Its failing status can never overwrite the positive status on X. |
| O (observed) | X, the corrected bootstrap commit: harmless reviewed template content, already in `main` history. |
| NONCE | A fresh UUIDv4, also used as the witness request's `request_id`. |
| P (pre-overlay) | `main` HEAD after R is pushed; its workflow blob must be `71e0271e…`. |

## 5. Procedure

Run the following from the local private clone (`<CLONE>`). `<TOOL>` is `tools/g1-witness/overlay.mjs` from the merged public repo. Both are local placeholders, not published paths.

1. **Anchor R.**
   - Run `git commit --allow-empty -m "G1 witness requested-identity anchor"`, then `git push origin HEAD:main` (normal fast-forward; never force).
   - Record `R=$(git rev-parse HEAD)` and `P=$R`.
2. **Plan (writes nothing).** Run `node <TOOL> plan <CLONE> --requested $R --observed $O --request-id $NONCE`. Keep the JSON: pre-overlay commit, baseline blob, overlay blob, expected refusal, and the exact delta.
3. **Isolate outcome evidence.** Proceed only while `busy=false` and no `Runner.Worker` process exists.
   - Move the workspace's `grl-outcome` folder, if present, to a dated checkpoint folder outside `_work`. The path is `<RUNNER_ROOT>\_work\github-runner-local-exec\github-runner-local-exec\grl-outcome`.
   - `run-profile` does not delete old files, so a stale positive `result.json` could otherwise ride along in the refusal artifact (tested).
   - Touch nothing else: no credentials, `.runner`, other workspaces, runner root or registration.
4. **Apply.**
   - Run `node <TOOL> apply <CLONE> --requested $R --observed $O --request-id $NONCE`; it prints the overlay blob, which must equal the plan's.
   - Commit only the workflow: `git commit -am "TEMPORARY G1 witness overlay (FAULT_INJECTED, acceptance-only): grl-target ref=<O>, nonce=<NONCE>"`.
   - Run `node <TOOL> verify-overlay <CLONE> ...` (it checks exact bytes, one-file scope and a baseline parent).
   - Push with a normal fast-forward and record overlay commit `Y`.
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
7. **Restore — always, even if step 6 failed, timed out or never started.**
   - An in-flight run keeps using Y's workflow, so restoring cannot alter it.
   - Run `node <TOOL> restore <CLONE> --requested $R --observed $O --request-id $NONCE`, then `git commit -am "Restore reviewed G1 dispatch workflow"`, then a normal push. Record restore commit `Z`.
8. **Verify normal operation.**
   - Run `node <TOOL> verify-normal <CLONE>`.
   - Run `git diff P Z` (it must be empty) and `git rev-parse Z:.github/workflows/grl-dispatch.yml`, which must be `71e0271e…`.
   - Confirm the four variables are unchanged and that no other workflow file exists.
   - From this point, normal mailbox requests (no nonce) are admitted by the reviewed workflow again.

## 6. Abort rules

Stop without dispatching if any tool check refuses, the runner is busy or offline, a run is queued, or the plan's overlay blob changes. If the overlay is already pushed, still run step 7 and step 8. Do not cancel an in-progress job or use Stop Now to speed things up. Never force-push, rewrite history, delete the tag or issues, or reuse the nonce. If reporting turns out incomplete, record `REPORTING_INCOMPLETE` honestly; do not rerun to manufacture a complete refusal.

## 7. Offline evidence for review

Run `node --test tools/g1-witness/overlay.test.mjs`. It covers:

- the pinned baseline equals the template workflow blob;
- the exact two-location delta;
- template lint passes on the overlay, and guard, pin, permission and label lines are identical;
- parameter, equal-SHA, UUID-injection, drift, CRLF and double-apply refusals;
- exact byte restoration and non-overlay refusal;
- the git-backed CLI plan/apply/verify/restore round trip to a tree identical to the pre-overlay commit;
- dirty-tree, non-commit and non-ancestor refusals;
- one-file commit scope;
- a real `git` checkout at O fed to the **unchanged** `grl-run-profile` entrypoint, which yields the exact `BLOCKED/CHECKOUT_SHA_MISMATCH` outcome with zero profile processes and no canonical result.
