# github-runner-local — OD-1/OD-2 approved; live bootstrap waiting on owner UI (V25)

## 1. Phase

**C_MERGED + E_MERGED + OD1_OD2_APPROVED + LIVE_BOOTSTRAP_WAITING_OWNER_UI.**

## 2. Merged source state

C merged via `451d3ab889f9f63f45ebf90ba54ddece7b60e6eb`.

E merged via `a296f3f9dfa362f6a53d20ff81a46eef7a519819`.

Both had independent exact-head PASS before merge.

## 3. OD-1 accepted

Owner approved creation/use of dedicated private execution repo:

`bohanyt/github-runner-local-exec`.

Existing project repos are not substitutes.

## 4. OD-2 accepted

Owner approved an acceptance-only GitHub App:
- owner controlled;
- Device Flow enabled;
- not public yet;
- runtime Client ID supplied to wizard;
- no embedded client secret/private key.

Proposed display name:
`github-runner-local-dev`.

## 5. Durable live packet

Issue #14:
`GRL-011 — Live acceptance bootstrap: owner-only GitHub App + private execution repo`.

Follow that packet for the first live same-repo acceptance.

## 6. Manual prerequisite boundary

Current ChatGPT GitHub connector cannot create a repository or register a GitHub App.

Owner must create both in GitHub UI.

Do not begin live runner registration until both exist.

## 7. Remaining boundaries

D21 Stage-2 credentials remains OPEN.

No:
- cross-repo checkout;
- hosted Actions;
- service/UAC runner;
- public App distribution;
- unreviewed external code;
- release work.

## 8. Next

Owner creates:
1. private repo `bohanyt/github-runner-local-exec`;
2. owner-only Device-Flow GitHub App.

Then provide the App Client ID or say both objects are ready.

Primary CT then verifies state and dispatches bounded D/G1 live activation.

END_OF_GRL_HANDOFF key=GRL-20260925-OD1-OD2-APPROVED-LIVE-BOOTSTRAP-V25 sections=8
