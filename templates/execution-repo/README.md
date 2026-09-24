# GRL execution repository template (Checkpoint E)

This is an **inactive source template** for a separate execution repository. Its nested `.github/workflows/grl-dispatch.yml` is not a workflow in the control repository. Copying it, enabling Actions, provisioning a runner, or sending a request requires separate owner decisions and activation proofs. Checkpoint E supplies source and local tests only; it does not run hosted Actions or prove report-only rerun behavior in GitHub Actions.

## Stage-1 boundary

The only allowed request target is the execution repository itself (`target.repository == GITHUB_REPOSITORY`). The workflow uses `issue_comment: created` with top-level `permissions: {}`. The `admit` job has a job-level prefilter for the configured mailbox issue, non-PR comment, authorized actor, `OWNER` association, and exact request marker. All four jobs require `[self-hosted, Windows, X64, grl-exec]`. There are no hosted runner labels, remote third-party actions, Stage-2 credentials, or cross-repository checkout.

Configure repository variables before a separately approved activation:

| Variable | Format | Purpose |
| --- | --- | --- |
| `GRL_MAILBOX_ISSUE` | Positive issue number | The one request mailbox |
| `GRL_AUTHORIZED_ACTORS` | JSON string array, e.g. `["owner"]` | Actors allowed to create requests and rerun |
| `GRL_ALLOWED_BRANCHES` | JSON string array, e.g. `["main"]` | Branches in which the requested SHA must be contained |
| `GRL_DISK_RESERVE_GIB` | Nonnegative integer | Free disk held in reserve |

The later runner activation must set `GRL_RUNNER_VERSION` to the actual installed runner version and `GRL_IDENTITY_CLASS` to `portable-user` or `service-account`; missing or noncanonical metadata blocks execution rather than producing an invented result.

The template has no token other than the job-scoped `GITHUB_TOKEN`. The admit job reads the original comment again, verifies the exact fenced request bytes and SHA/profile identity, checks branch containment, checks replay state, and posts a bounded ACK. Profiles are reviewed JSON data with an executable and `argv` array; no request field selects a machine, shell, script, or arbitrary command. The only bundled profile is `js-smoke`. The profile definition SHA is the Git blob SHA-1 of its exact bytes, so profile files are excluded from line-ending conversion by `.gitattributes`.

The execute job checks the target checkout HEAD before any profile process. If it differs from the requested SHA, it runs **zero** profile processes, creates **no** canonical `result.json`, and sends a bounded internal `BLOCKED / CHECKOUT_SHA_MISMATCH` outcome. Report then posts `<!-- grl-exec-refusal v1 -->` (at most 8 KiB) and a failing `grl/js-smoke` commit status on the requested SHA. It never asserts `tested_sha` for this refusal. Actually executed outcomes use the canonical `<!-- grl-result v1 -->` contract (at most 16 KiB comment, 32 KiB result). A test failure is execution data. The final verdict succeeds only for admitted, exact-SHA `PASS` with both comment and commit status published; every refusal or incomplete report fails.

Results and status publications retry independently, at most three attempts. The execute job also uploads a bounded three-day outcome artifact and a separate refusal marker artifact for E-B1. Report can consume the outcome artifact if job outputs are unavailable; reconciliation uses the refusal marker to classify a concluded run with missing refusal publication as `REPORTING_INCOMPLETE`. The observer distinguishes complete E-B1 refusal from interrupted or partially reported runs. Reconciliation is bounded to the recent mailbox window. GitHub Actions runtime proof, especially report-only rerun behavior after an interrupted run, remains an activation prerequisite and is **not** claimed by these local source checks.

## Local checks

From the control repository root, with Node 20 or later:

```text
node --version
node --test templates/execution-repo/tests/*.test.mjs
node templates/execution-repo/tools/lint.mjs
node templates/execution-repo/tools/fixture-proof.mjs pass
node templates/execution-repo/tools/fixture-proof.mjs fail
node templates/execution-repo/tools/fixture-proof.mjs refusal
node templates/execution-repo/tools/schema-check.mjs
git diff --check
```

The linter checks the workflow's trigger, permissions, prefilter, runner labels, immutable first-party action pins, checkout credentials, shell expressions, Stage-2 leakage, profile shape, report/verdict dependency, refusal routing, schema byte mirror, and changed-path scope. The `tests/` folder includes negative mutations of these rules. The mirrored request/result schemas under `schemas/` must be byte-for-byte equal to the control repository's root schemas; `schema-check.mjs` fails on drift. No npm install is needed.

## Activation prerequisites

An independent exact-head review and owner decision must precede any copy to a private execution repository. Activation must separately establish a dedicated non-elevated Windows runner with the exact label set, a mailbox issue, repository variables, branch and actor policy, runner isolation, token permission behavior, and real GitHub Actions reporting/retry proofs. The control repository's root workflows and schemas stay outside this template's scope. The template is not activated by this Checkpoint E publication.

Later activation must also create and review fixture commits in the private execution repository, record their exact PASS and FAIL SHAs, and verify dispatch against those SHAs. This public template checkpoint creates no private repository or private fixture commits.
