# github-runner-local — PR #22 reopen/resume independent review (V46)

## 1. Phase and authority

**G1_OFFICE_PASS_CT_ACCEPTED; GRL015_PR22_INDEPENDENT_REVIEW_READY.**

Active task remains Issue #21 / GRL-015. Active review packet is Issue #21 comment `5842772958`, read IN FULL through:
`END_OF_GRL015_REVIEW_PACKET key=GRL-PR22-7D4FF0F-REOPEN-REVIEW-20260926 sections=6`.

DRAFT PR #22 is frozen for review:
- branch `feat/grl-015-planned-reopen-resume`
- head `7d4ff0f07d4b93562932469b48d1310069f8dc91`
- source base `3ae26872430c0aded342d15cdbac3a315ecb2f8b`
- one commit / 12 changed paths.

Implementation handoff `5842736701`; implementer claim/release `5842551433` / `5842738309`.

## 2. Evidence to retain

Implementer reports:
- build clean;
- Integration 170/170;
- Presentation 64/64;
- Core 192/192;
- six targeted safety mutations caught.

These are implementation LOCAL_CHECKED results until independently reproduced.

No live runner mutation occurred during implementation. The proven G1 office runner/wizard/root/private repo remain preserved.

G1 remains accepted and is not part of this review.

## 3. Review scope

ONE independent reviewer, different from implementer and CT, checks exact PR #22 head under packet `5842772958`.

Primary questions:
- only eligible planned Paused/no-survivor exact-ID state can resume;
- exact remote identity is offline/non-busy before start;
- existing-root path can never configure/re-register or request registration token;
- listener-only version verification and run.cmd-only start;
- no duplicate concurrent/repeated process start;
- truthful persisted state across failures/timeouts/Stop Now;
- no adoption of online unowned process;
- UI offers Resume existing runner only when eligible;
- first-install/removal/Core provenance fences remain intact.

Focused affected tests only; no G1 rerun, hosted Actions, old D campaign, UI smoke or live runner operation at this gate.

## 4. Sequence

**SEQUENTIAL:** independent source PASS -> separate CT merge/resume -> SAME local Opus planned close/relaunch live acceptance -> planned reboot acceptance -> Issue #18 switching.

Reviewer publishes one exact-head result on Issue #21 and releases claim, then stops.

Do not Stop Now, close the wizard, relaunch, reboot or mutate the private execution repo before CT opens the live gate.

## 5. Safety

ARCHIVE/CHECKPOINT FIRST. Preserve runner registration/root/credentials/history and all G1 evidence.

No service/autostart, security/sleep-policy changes, hosted runners, personal runner, arbitrary/cross-repo/Stage-2 execution or global auth/env reset.

NF-2 remains deferred unless a concrete review finding shows it directly blocks this path.

END_OF_GRL_HANDOFF key=GRL-20260926-PR22-REOPEN-REVIEW-V46 sections=5
