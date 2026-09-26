# github-runner-local — PR #22 merged; live reopen/reboot acceptance (V48)

## 1. Phase and authority

**G1_OFFICE_PASS_CT_ACCEPTED; GRL015_PR22_MERGED_LIVE_ACCEPTANCE_READY.**

PR #22 corrected head
`22840674b95129dc0dd0668e5ac12188ddfadbbd`
is merged at:
`34ccb0c999873418a5077bd3da793a460901a968`.

Owner explicitly directed CT to open the gate without the previously-required
second focused rereview. The override is recorded in Issue #21 comment
`5842966085`.

This is an owner-directed CT process override, NOT an independent review PASS.

Active live packet: Issue #21 comment `5842973464`, FULL through:
`END_OF_GRL015_LIVE_GATE key=GRL-PR22-MERGED-LIVE-REOPEN-REBOOT-20260926 sections=6`.

## 2. Source evidence and correction history

Initial independent review `5842855807` accepted the broad planned-reopen design
and found one SAFETY_BLOCKER: redirected `runner\bin` junction.

The SAME implementer corrected only two paths; handoff `5842938400`, release
`5842939599`. Worker reports:
- Integration 173/173 twice;
- real Windows junction regression;
- redirect introduced after verification refused at start;
- two targeted mutations caught;
- Presentation/lifecycle/Core/docs unchanged by the correction.

CT sanity-inspected the exact two-file correction before the owner-directed merge.

G1 remains accepted and closed.

## 3. Live sequence

SAME local Opus takes ONE broad Issue #1 claim and proceeds sequentially:

checkpoint -> fresh runner/queue checks -> build merged product away from the
running wizard -> explicit Stop Now -> verify ID 2 offline + Paused/no-survivor ->
close old wizard -> launch merged product with SAME root/repo/name -> sign in if
needed -> Resume existing runner -> SAME ID 2 online -> one ordinary js-smoke PASS.

That produces **REOPEN_RELAUNCH_PASS**.

Only then:
checkpoint -> Stop Now -> Paused/no-survivor -> close -> owner-visible Windows
reboot boundary -> after boot manual merged product launch -> sign in if needed ->
Resume existing runner -> SAME ID 2 online -> one ordinary js-smoke PASS.

That produces **GRL015_REOPEN_REBOOT_PASS**.

No new reviewer or CT micro-gate between ordinary steps.

## 4. Reboot interaction

Do not surprise-reboot an actively used machine. At the actual reboot boundary the
local Opus should tell the owner the machine is checkpointed and ready. The agent
session may stop during reboot; resume the SAME goal/session using GitHub authority
and the non-secret local checkpoint after boot.

Manual launch after boot is sufficient; no service/autostart is required.

## 5. Safety / next phase

ARCHIVE/CHECKPOINT FIRST. Preserve exact runner/root/registration/credentials and
all accepted G1/private history.

No delete/reset/reinstall, no `--replace`, no hosted Actions, service/autostart,
security/sleep changes, personal runner, Stage-2/cross-repo/arbitrary jobs or
global auth/env reset.

If both live witnesses pass, Issue #21 can be accepted and the next phase is
Issue #18 office/personal one-active-at-a-time switching. NF-2 remains deferred.

END_OF_GRL_HANDOFF key=GRL-20260926-PR22-MERGED-LIVE-ACCEPTANCE-V48 sections=5
