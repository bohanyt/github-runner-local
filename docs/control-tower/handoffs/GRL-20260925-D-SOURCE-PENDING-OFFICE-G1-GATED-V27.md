# github-runner-local — Checkpoint D source pending before office G1 (V27)

## 1. Phase

**D_SOURCE_IMPLEMENTATION_READY; OFFICE_G1_GATED.**

Owner approvals for the office laptop and conditional personal laptop remain accepted. The merged product has no production live runner lifecycle path yet; Issue #15 is not ready to execute.

## 2. Source and owner decisions

Public source: `bohanyt/github-runner-local` main.

C and E were merged at commits `451d3ab889f9f63f45ebf90ba54ddece7b60e6eb` and `a296f3f9dfa362f6a53d20ff81a46eef7a519819`.

OD-1/D09: dedicated private execution repo `bohanyt/github-runner-local-exec` accepted.

OD-2/D10: owner-only Device Flow App for acceptance accepted; runtime Client ID is recorded on Issue #14. App installation settings remain owner-attested.

OD-7/D29: current office Windows laptop authorized as portable, interactive, non-admin, trusted-code-only runner `grl-office`.

D30: after G1 PASS, personal laptop `grl-personal` may be enrolled separately. When both carry `grl-exec`, exactly one runner process may be active at a time. Never copy credentials between machines.

## 3. Sequencing correction

Checkpoint B's WPF composition still uses fake adapters. Checkpoint C provides device flow, package and CLI contracts but does not yet wire live administration, registration-token acquisition, `config.cmd`/`run.cmd` process execution, portable lifecycle, or a live wizard.

Therefore the earlier V26 instruction to execute live Issue #15 immediately is superseded. No manual shell enrollment may stand in for the missing product source.

## 4. Current source task

Issue #16, GRL-013, is the bounded Checkpoint D implementation packet. Its CT Windows batch-launch clarification is comment `5826696683`.

One implementation worker may claim the D source task, work within the Issue #16 allowlist, validate on Windows, open a DRAFT PR, publish evidence, release and stop. A different reviewer must inspect the exact head. CT evaluates the result and any merge separately.

No live device login, runner registration/start, private template activation, or G1 execution is authorized during D source work.

## 5. Deferred live task

Issue #15, GRL-012, remains the owner-authorized office G1 packet but is **GATED** until D source implementation, independent review and merge are complete, followed by CT read of exact main and explicit dispatch.

The live G1 witness then requires real Device Flow, exact private repo selection, unelevated runner online, same-repo harmless fixture, request/ACK/result/verdict, safe negative witnesses and no hosted runner. Evidence remains LOCAL_CHECKED until the real witness supports WINDOWS_TESTED.

## 6. Boundaries and stop conditions

No GitHub-hosted Actions, Stage-2/cross-repo credentials or checkout, public App distribution, service/UAC/elevated mode, arbitrary shell inbox, external project workloads, signing/release, or personal-laptop enrollment before G1 PASS.

Stop D source work if the fixed runner CLI cannot be safely invoked on Windows, a new dependency or scope expansion is required, authority/head changes, or another worker owns the claim. Publish a blocked result rather than activate live steps.

## 7. Control Tower ownership

Owner explicitly designated this CT as successor and deprecated the previous CT. Claim `5826687223` supersedes the unfinished earlier CT sequencing claim `5826550383`. The present handoff and CURRENT are the successor authority; Issue #16 packet and clarification define worker scope.

## 8. Next

Dispatch ONE bounded D source implementation worker using Issue #16. On worker handoff, CT verifies branch/head, proof and boundaries; dispatches a different independent reviewer; considers merge only after PASS; returns to Issue #15 for office G1 only after D is merged.

END_OF_GRL_HANDOFF key=GRL-20260925-D-SOURCE-PENDING-OFFICE-G1-GATED-V27 sections=8
