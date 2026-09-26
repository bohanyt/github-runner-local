# CURRENT — github-runner-local

Updated: 2026-09-26. Phase: **G1_OFFICE_PASS_CT_ACCEPTED; GRL015_PR22_MERGED_LIVE_ACCEPTANCE_READY**.

## Authority

- Canonical branch: main; continuing owner-designated Control Tower coordinates on Issue #1.
- Handoff: `docs/control-tower/handoffs/GRL-20260926-PR22-MERGED-LIVE-ACCEPTANCE-V48.md`.
- Sentinel: `END_OF_GRL_HANDOFF key=GRL-20260926-PR22-MERGED-LIVE-ACCEPTANCE-V48 sections=5`.
- Active task: Issue #21 / GRL-015.
- Active live packet: Issue #21 comment **`5842973464`**, FULL through `END_OF_GRL015_LIVE_GATE key=GRL-PR22-MERGED-LIVE-REOPEN-REBOOT-20260926 sections=6`.

## Merge / override

PR #22 corrected head
`22840674b95129dc0dd0668e5ac12188ddfadbbd`
is MERGED at
`34ccb0c999873418a5077bd3da793a460901a968`.

Owner directed CT to open the gate and waive the second focused rereview of the
narrow junction correction. This is recorded in Issue #21 `5842966085` as
`OWNER_OVERRIDE_NO_SECOND_REREVIEW`; it is NOT an independent review PASS.

The first independent review remains authoritative for all unchanged reopen
behavior; its sole blocker was corrected in the merged two-file delta.

## Live goal

SAME local Opus now owns one broad live phase:

1. checkpoint and fresh runner/queue checks;
2. build merged product away from currently-running wizard binaries;
3. explicit warned Stop Now;
4. verify runner ID 2 offline + persisted Paused/no-survivor;
5. close old wizard;
6. launch merged product against SAME root/repo/name;
7. normal sign-in if required;
8. Resume existing runner;
9. verify SAME ID 2 online, no duplicate registration;
10. one harmless js-smoke PASS -> REOPEN_RELAUNCH_PASS;
11. checkpoint/Stop Now again;
12. planned owner-visible reboot boundary;
13. after boot manual launch/sign-in/resume SAME ID 2;
14. second harmless js-smoke PASS -> GRL015_REOPEN_REBOOT_PASS.

No reviewer/CT handoff between these ordinary steps.

## Safety

ARCHIVE/CHECKPOINT FIRST. Preserve runner/root/credentials/private history.

No G1 rerun, PR #19, hosted Actions, service/autostart, security/sleep changes,
personal runner, Stage-2/cross-repo/arbitrary jobs, global auth/env reset,
delete/reset/reinstall or `--replace`.

At the actual Windows reboot boundary, tell the owner the machine is ready rather
than surprise-rebooting an actively used system.

After both witnesses pass: CT accepts Issue #21, then Issue #18 switching is next.
NF-2 remains deferred.
