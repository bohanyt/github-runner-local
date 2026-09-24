# Acceptance and implementation gates

Status: PROPOSED. This file defines future evidence, not results already obtained.

## Gate 0 — Current bootstrap

Main authority and full handoff exist; planning issue and draft design are linked; preview and fixtures are clearly mock. Local document/example checks pass. No workflows, tokens, runner registration, installer or Windows state changes are shipped.

## Gate 1 — Independent plan

GRL-001 resolves or explicitly blocks topology, auth, runtime, service identity/layout, comment authorization, secrets, protocol and test strategy. Verdict: `PLAN_READY_FOR_REVIEW` or `NEEDS_DECISION`. Owner/reviewer acceptance is required before implementing. A diagram alone is insufficient.

## Gate 2 — Local non-network core

Suggested first worker scope: wizard state machine, validated non-secret configuration, filesystem ownership checks and fake adapters. No live auth/service/runtime download. Tests cover validation, cancel/retry, account switching, redirected paths, low disk and partial progress. A first prototype must label simulated states.

## Gate 3 — Approved Windows setup pilot

On an explicitly authorized Windows machine: real GUI startup, DPI/keyboard use, actual shell/tool-policy preflight, supported runtime, secure credential store, browser/device cancellation, permission-denied repo, verified download and rollback. Test UAC consent/denial separately from application-control restrictions. No claim based solely on another application working.

Before service testing, prove protected binaries/ancestors and the non-admin job identity. Resolve the Documents exception with the owner. Test service install/start/stop/remove, reboot, interrupted setup and cleanup. Portable mode must accurately state that user logoff/app exit stops its runner.

## Gate 4 — One real GitHub round-trip

Owner chooses a private test scope and harmless exact-SHA fixture. Register one runner with explicit consent. The active chat connector publishes a request. Record actual sender/event, run/attempt/job identity, non-admin execution identity, checkout SHA, profile definition SHA, exit code, counts, result comment and optional artifact.

Agent rereads the result through the connector and verifies identity. No manually pasted log is required. Then test a real failure: result must say FAIL with evidence, not a green summary caused by a reporting step.

## Gate 5 — Negative and recovery campaign

| Case | Required observation |
|---|---|
| Unauthorized/fork/public actor | No target code executes. |
| Malformed request or shell text | Rejected as data; no interpolation. |
| Expired/edited/replayed request | Rejected or reconciled without duplicate work. |
| Wrong target/workflow SHA | Clear mismatch; no substitute checkout. |
| UAC denied / no admin | No privileged operation; portable mode only if separately supported. |
| Shell/tool blocked | Honest blocked status; no policy weakening. |
| Disk low / fills during job | Bounded failure; no deletion outside owned paths. |
| Offline / sleep / lost connection | Queued/incomplete state, safe reconciliation; no invented PASS. |
| Cancel / process child survives | Bounded process-tree policy with observable outcome. |
| Report upload failure | Execution/report outcomes stay separate; retry does not rerun tests accidentally. |
| Forged report / wrong attempt | Rejected by controller identity check. |
| Switch account/laptop / duplicate name | Distinct registrations; explicit removal and credential scope. |
| Update tamper / partial update | Verification failure and recoverable prior state. |
| Uninstall offline | Pending remote removal is explicit; local secrets/workspace choices are separate. |

## Gate 6 — Real project adoption and release

Audit each selected project's workflows for Windows compatibility before enabling its profile. No copying Linux Docker service jobs onto native Windows. Interactive desktop/game/hardware acceptance remains separate from CI and services.

Measure cold/warm runs on the same target/profile: queue, checkout/restore, build/test, publish, peak RAM and remaining disk. Do not promise faster execution without a comparable baseline. Resolve license, signing/provenance, packaged assets, checksums, supported updater and recovery documentation. Only then request owner release approval.

## Evidence format

Each report identifies task, source head, toolchain, environment, commands/procedure, expected and actual result, evidence locations, limitations and reviewer. Separate temporary Linux/mock validation from physical Windows acceptance. No release is authorized by this design pack.
