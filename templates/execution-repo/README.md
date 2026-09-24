# GRL execution-repository template (inactive source)

This directory is **source only**. It is nested under `templates/execution-repo`, so GitHub does not activate its workflow in `github-runner-local`. Checkpoint E does not create or use a private execution repository and does not dispatch Actions.

## Stage-1 boundary

Stage 1 accepts only owner-authored requests on one mailbox issue, targets the execution repository itself, requires exact 40-hex SHAs contained in reviewed allowlisted branches, and executes only reviewed data-only profiles. This is **trusted-code-only**, not a sandbox: code on an allowed branch can still be malicious and a compromised owner/connector session can authorize code execution as the runner identity. No PAT, installation token, multi-repo checkout, arbitrary command field, source write-back, service mode, or runner activation is present here.

Configure the eventual reviewed private copy with repository variables `GRL_MAILBOX_ISSUE` (JSON number), `GRL_AUTHORIZED_ACTORS` (JSON array of logins), `GRL_ALLOWED_BRANCHES` (JSON array, initially `['main']` in valid JSON double-quote form), and `GRL_DISK_RESERVE_GIB`. The later runner activation must also set process environment `GRL_RUNNER_VERSION` to the actually installed runner version; admission rejects missing/noncanonical runner metadata rather than inventing a version. Activation must happen only under a later authorized packet because `issue_comment` workflows must exist on the default branch to receive events.

## Workflow

`grl-dispatch.yml` has four jobs: `admit`, `execute`, `report`, `verdict`. Every job uses `[self-hosted, Windows, X64, grl-exec]`. The admission job has a job-level mailbox/PR/actor/OWNER/marker filter so non-matching comments reserve no self-hosted job. Remote actions are first-party only and pinned to full commit SHAs. Checkout credentials are never persisted. The bounded execution outcome is transferred with a 3-day artifact; runtime proof that a GitHub **re-run failed jobs** operation reuses successful execution output without rerunning tests remains **UNVERIFIED until activation**.

Pinned source refs verified for this checkpoint: `actions/checkout` `11d5960a326750d5838078e36cf38b85af677262`, `actions/upload-artifact` `ea165f8d65b6e75b540449e92b4886f43607fa02`, and `actions/download-artifact` `d3f86a106a0bac45b974a628896c90dbdf5c8093`.

## Exact-SHA refusal

If request SHA A was admitted but checked-out HEAD is B, `grl-run-profile` spawns **zero** profile processes and emits no canonical `grl.result.v1`. It records bounded internal `BLOCKED / CHECKOUT_SHA_MISMATCH` data. Reporting publishes `<!-- grl-exec-refusal v1 -->` with requested A, observed B, and `profile_executed:false`, plus a failing `grl/<profile>` commit status on A. It never contains `tested_sha` or test counts. Complete refusal publication is terminal BLOCKED; partial/missing refusal reporting is REPORTING_INCOMPLETE. The final verdict always fails for BLOCKED.

## Profiles and fixtures

Profiles are strict JSON, data-only, and execute an `executable` plus an `argv` array with `shell:false`. `profile.definition_sha` is the Git blob SHA of the exact profile bytes at the workflow commit. `js-smoke-pass` and `js-smoke-fail` are dependency-free deterministic fixtures that write JUnit XML; the failing fixture is test data, not a harness/infrastructure failure.

## Development proof

From the public product repository root, with Node 20+:

```text
node --test templates/execution-repo/tests/*.test.js
node templates/execution-repo/tools/check-schema-mirrors.js .
node templates/execution-repo/tools/lint-template.js
```

For branch-scope checking, set `GRL_LINT_BASE` to the exact base commit before running the linter from a real git checkout. Also run `git diff --check <base>..HEAD` and verify every changed path begins `templates/execution-repo/`.

No npm install is used. The tests use only Node built-ins. Schema mirrors are required to be byte-for-byte equal to root merged schemas.

## Activation proofs still required later

A later authorized activation must create reviewed fixture commits in the private execution repo and prove the connector-authored owner comment actually triggers the default-branch workflow, real Windows runner behavior, exact-SHA refusal publication/status, report-rerun behavior, pause/disconnect/reconciliation, and the owner-acceptance campaign. Nothing in this source checkpoint is `WINDOWS_TESTED` or `OWNER_ACCEPTED`.
