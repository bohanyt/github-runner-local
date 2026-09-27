# github-runner-local — A1 owner-authorized ID3 live witness (V58)

## 1. Phase and authority

**GRL014_SOURCE_MERGED; A1_ID3_LIVE_WITNESS_AUTHORIZED; A5_ODG_GATED; GRL015_ACCEPTED.**

Owner explicitly approved the bounded A1 witness on office runner ID 3.

Active live packet: Issue #18 comment `5856141281`, read IN FULL through:
`END_OF_GRL014_A1_LIVE_PACKET key=GRL014-A1-ID3-LIVE-WITNESS-20260927 sections=6`.

Isolated A1 attempt Issue #18 `5856076011` concluded `A1_ISOLATED_INSUFFICIENT_REAL_JOB_REQUIRED`; claim released by `5856083361`.

## 2. Authorized witness

Exactly one fresh local Windows operator/proof worker may:
- checkpoint/archive current accepted ID-3 state first;
- install two temporary reviewed hook pointers in `.env` reversibly;
- restart the same registered Listener at confirmed idle;
- run exactly two sequential existing `js-smoke` requests: PASS then REFUSE-execute;
- collect Listener -> real Worker -> hook/order evidence;
- restore `.env` byte-exactly and restart the same ID 3;
- publish one result and stop.

No re-registration, no numeric-ID change, no private workflow mutation, no reboot and no third request.

## 3. Required proof

A1 PASS requires empirical real-chain evidence:
- runner-root `.env` is the hook source;
- actual pinned Listener launches actual pinned Worker;
- hook marker binds to the real Worker;
- PASS hook occurs before ordinary/default steps and js-smoke executes;
- REFUSE execute hook exits nonzero before default/profile steps and profile does not execute;
- rollback restores the pre-witness configuration exactly and same registration ID 3 returns online/idle.

No PASS from source inspection, fake Worker, manually inherited environment or missing-file inference.

## 4. Stop / recovery

Hard-stop before mutation on identity/version/hash drift, foreign hooks, ambiguous `.env`, checkpoint failure, duplicate registration, nonterminal/unexpected work or claim conflict.

During witness stop on unexpected delivery, mismatched identity, stale marker, inability to bind process chain, default-profile execution after refusal, env drift, stuck nonterminal run or need for a third request.

Do not kill stuck witness work. Preserve evidence and report BLOCKED/RECOVERY_REQUIRED.

Rollback failure never permits re-register/remove/re-enroll improvisation.

## 5. Standing boundaries

A5/OD-G/personal enrollment/cross-machine switching remain unauthorized.
Normal GRL-014 product live activation remains gated beyond this A1 witness.
G1/GRL-015 stay accepted.
No hosted Actions, reboot, service/autostart/security/sleep changes, global auth/env reset, destructive cleanup or credential exposure.
ARCHIVE/CHECKPOINT FIRST.

END_OF_GRL_HANDOFF key=GRL-20260927-A1-ID3-LIVE-AUTHORIZED-V58 sections=5