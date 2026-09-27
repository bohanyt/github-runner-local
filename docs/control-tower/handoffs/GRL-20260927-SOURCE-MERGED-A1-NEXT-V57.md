# github-runner-local — GRL-014 source merged; A1 proof next (V57)

## 1. Phase and authority

**GRL014_SOURCE_MERGED; A1_PROOF_PENDING; A5_ODG_GATED; LIVE_ACTIVATION_GATED; GRL015_ACCEPTED.**

Source acceptance: Issue #18 comment `5855825625`, through:
`END_OF_GRL014_SOURCE_ACCEPTANCE key=GRL014-PR24-MERGED-20260927 sections=5`.

PR #24 reviewed head `90df67d1d9bdf7b1987fcaee27056510bd2240e4` merged at
`25b1ecf60baa9fc9ef132348f9e771f44b8cb7b5`.

Main contains the reviewed source implementation.

PR #23 remains open/draft design reference at `ccd580aec36432c4b40eeedecf3a04fab17f9a97`; it is not live authority.

## 2. Accepted source state

Independent review chain closed all source blockers:
- F1 orphan Worker path containment PASS;
- F2 full jobs/steps inventory PASS;
- F3 stale-lock warned Stop Now PASS;
- F4 independent detection evidence PASS.

Prior accepted unchanged-source conclusions remain retained.

No additional source/design review loop is required absent a concrete regression.

Normal live composition remains disabled; existing runner roots are not auto-migrated.

## 3. Remaining proof

A1 remains PENDING: actual pinned Windows Runner.Worker inheritance of runner-root `.env` hook variables and real hook refusal ordering have not been witnessed.

A5 remains PENDING: live Git ref CAS behavior is not witnessed and is additionally gated by pending OD-G.

Do not treat source merge as WINDOWS_TESTED or OWNER_ACCEPTED live switching.

## 4. Next sequence

**SEQUENTIAL:** choose/authorize smallest A1 witness -> execute A1 proof -> CT evaluates live gate. A5 waits for explicit OD-G and control-repo authorization.

Preferred A1 approach preserves office ID 3 and its accepted root first. Attempt an isolated pinned-runner witness if it can genuinely exercise Listener -> Worker inheritance; do not substitute a manually inherited environment or fake Worker for A1.

If isolated A1 cannot establish the contract, a checkpointed office-ID-3 witness requires a separate explicit owner authorization before any `.env`/hook/root/restart/job mutation.

## 5. Safety

G1 and GRL-015 remain accepted. Office runner `grl-office` ID 3 stays untouched until a later explicit proof packet.

No control repo, App permission, personal enrollment, remote takeover or scheduled keep-alive.
No hosted Actions, service/autostart/security/sleep changes, global auth/env reset, destructive cleanup or credential exposure.
ARCHIVE/CHECKPOINT FIRST.

END_OF_GRL_HANDOFF key=GRL-20260927-SOURCE-MERGED-A1-NEXT-V57 sections=5