# Request/result protocol proposal v0

**DESIGN ONLY.** Examples are invented, marked `example_only`, and must never be accepted by a future live dispatcher. No trigger is installed in this repository.

## Dispatch route

Candidate: the chat agent posts a structured request comment to an approved issue in a private execution repository through its GitHub connector. A reviewed workflow on that repository's default branch listens to created issue comments, validates the actual actor and payload, then routes one job to the eligible self-hosted runner.

R5: issue-comment context points at the default branch, not automatically the requested source commit. R6: the workflow token is repository-scoped and has event-recursion restrictions. Prove the actual connector comment sender and event behavior with a harmless approved smoke request; do not assume the connector exposes workflow_dispatch or that every bot-generated comment will dispatch.

There is no direct ChatGPT-to-laptop terminal. A completed workflow does not automatically wake an idle chat. An active agent can read the report; a later agent can resume from GitHub. Notifications require a separately supported, explicitly configured mechanism.

## Data contract

See [request example](examples/request.json). The future strict schema should reject extra fields, noncanonical repository identifiers, missing source SHA, arbitrary shell text, path traversal and invalid profiles.

Required identities: schema version; request UUID; authorized request repository/issue/comment; target repository and full commit SHA; approved profile and its trusted definition revision; expected registration/machine selector; expiry and timeout. GitHub-derived sender, comment ID, issue and timestamps must be verified from the event/API, not trusted from comment body claims.

Proposed initial profile: `smoke-fixture`, a known safe test fixture. `unit`, `build` and `full-ci` remain future allowlisted profiles requiring per-project Windows/tooling review. A profile is not a sandbox.

## Before executing

Verify sender's current authority, private/trusted scope, exact request bytes/body digest, fresh expiry, target approval and SHA, profile identity, machine capability/disk reserve and a non-elevated job user. Ignore edited/deleted/reporter comments. Obtain the exact source commit and compare actual checkout SHA, without silently substituting branch head or a merge ref.

Design durable idempotency keyed by request ID plus source event identity and payload digest. Reusing an ID with different contents is rejected. Retry must identify the previous run/attempt and explain whether execution or only publication is retried. GitHub concurrency groups are not automatically a durable FIFO queue or exactly-once transaction log.

## Results

See [result example](examples/result.json). Separate **execution_status** from **reporting_status**. Preserve request UUID, target and actual tested SHA, workflow definition SHA, run ID, job ID, attempt, runner identity, profile version, timestamps, tool versions and actual exit codes/test counts.

GitHub receives normal run/job status and logs through Actions. The custom implementation should additionally create/update one bounded authenticated result comment and optionally upload explicit artifacts. Artifacts require retention/size limits and may consume GitHub storage even with self-hosted compute. Do not assume job summaries are conveniently exposed by every connector; issue comments are the primary agent-readable return candidate.

Publish test results on failure when possible. Overall execution failure must not be hidden by a successful report step. Conversely a completed test with failed publication is `REPORTING_INCOMPLETE`, not delivery success. On power loss no code can publish; reconcile incomplete runs after reconnect. Missing/skipped tests are not PASS.

## Result trust and privacy

The control tower verifies author identity and actual run/attempt/target evidence, not only the words PASS in a comment. A malicious job on the same host can forge or steal reporting credentials; address this in the trust model rather than promising attestation.

Initial output is status/log + bounded comment. No automatic commit/push. The hub cannot post to a different private repo using its default GITHUB_TOKEN; it needs an explicit granted credential, or the chat agent links the hub result from the target task. No private source/logs are copied into the public installer repository.

## Cancellation and offline cases

A cancel request must reference one known request/run and have the same authority checks as dispatch. Stopping the UI, pausing admission, cancelling a job and unregistering a runner are distinct. If the runner is offline, report queued/unknown until GitHub or reconciliation supplies evidence. Never reroute to a paid hosted runner silently.
