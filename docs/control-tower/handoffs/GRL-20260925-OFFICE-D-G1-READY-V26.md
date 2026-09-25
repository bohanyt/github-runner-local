# github-runner-local — Office D/G1 ready (V26)

## 1. Phase

**OFFICE_D_G1_READY.**

All owner gates required for first same-repo portable acceptance are approved.

## 2. Approved live topology

Execution repo:
`bohanyt/github-runner-local-exec`.

GitHub App:
owner-only acceptance App, Device Flow enabled.

First runner:
`grl-office`.

Machine:
CURRENT OFFICE WINDOWS LAPTOP.

Mode:
portable, non-admin, trusted-code-only.

## 3. Exact packet

Issue #15:
`GRL-012 — Live D/G1 office-laptop portable enrollment`.

Follow it exactly.

## 4. First acceptance boundaries

Only owner-authored harmless fixture in the SAME private execution repo.

No Stage-2 cross-repo checkout.

No hosted Actions.

No service/UAC.

No arbitrary shell.

## 5. Second runner policy

After G1 PASS only:
- personal runner `grl-personal` may be registered;
- office + personal may both remain registered;
- while both share `grl-exec`, exactly ONE is active at a time;
- never copy runner credentials between machines.

## 6. Stop conditions

Stop on:
- wrong GitHub account;
- wrong/private/admin repo mismatch;
- elevated runner;
- runner pin/hash/capability mismatch;
- template semantic change requirement;
- hosted-runner requirement;
- cross-repo access;
- secret/token leakage;
- unexpected second active matching runner.

## 7. Evidence

G1 must include real Device Flow, runner-online, exact-SHA request, ACK, execution, result/verdict, no-hosted-runner proof, and safe negative witnesses.

No evidence inflation.

## 8. Next

One local office-laptop worker claims Issue #15, performs the bounded live D/G1 campaign, publishes one handoff with disposition, releases, and stops.

END_OF_GRL_HANDOFF key=GRL-20260925-OFFICE-D-G1-READY-V26 sections=8
