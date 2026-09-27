# CURRENT — github-runner-local

Updated: 2026-09-27. Phase: **GRL014_SOURCE_MERGED; A1_ID3_LIVE_WITNESS_AUTHORIZED; A5_ODG_GATED; GRL015_ACCEPTED**.

## Authority

- Canonical branch: main; Control Tower coordinates on Issue #1.
- Handoff: `docs/control-tower/handoffs/GRL-20260927-A1-ID3-LIVE-AUTHORIZED-V58.md`.
- Sentinel: `END_OF_GRL_HANDOFF key=GRL-20260927-A1-ID3-LIVE-AUTHORIZED-V58 sections=5`.
- Active task: owner-authorized bounded A1 live witness on existing office runner ID 3.
- Active packet: Issue #18 comment **`5856141281`**, FULL through `END_OF_GRL014_A1_LIVE_PACKET key=GRL014-A1-ID3-LIVE-WITNESS-20260927 sections=6`.

## Source state

GRL-014 source is merged and independently reviewed. PR #24 reviewed head `90df67d1d9bdf7b1987fcaee27056510bd2240e4` merged at `25b1ecf60baa9fc9ef132348f9e771f44b8cb7b5`.

F1/F2/F3/F4 are accepted closed. No source loop is active.

## A1 live witness authorization

Owner explicitly authorizes:
- ARCHIVE/CHECKPOINT FIRST;
- reversible temporary `.env` hook pointers and temporary hooks outside root/_work;
- same registered office runner `grl-office` numeric ID 3 only;
- minimum idle Listener restarts required for install/restore;
- exactly two sequential existing `js-smoke` requests: PASS then REFUSE-execute;
- byte-exact `.env` restore and final same-ID-3 online/idle verification.

No re-registration/removal, no third request, no workflow mutation, no reboot, no busy-worker kill.

Isolated proof `5856076011` established that a real registered job is required; it did not touch ID 3.

## Remaining gates

A1 remains pending until this witness is completed and CT adjudicates it.
A5 remains PENDING and gated by OD-G.
Personal enrollment and cross-machine switching remain unauthorized.

## Safety

Stop on any identity/version/hash/config drift, foreign hooks, checkpoint failure, nonterminal/unexpected work, unexpected job, uncertain process chain, failed rollback or need to widen scope.

G1/GRL-015 remain accepted.
No hosted Actions, service/autostart/security/sleep changes, global auth/env reset, destructive cleanup or credential exposure.
ARCHIVE/CHECKPOINT FIRST.