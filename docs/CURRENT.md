# CURRENT — github-runner-local

Updated: 2026-09-26. Phase: **G1_OFFICE_PASS_CT_ACCEPTED; GRL015_LIVE_MANUAL_UI_FALLBACK_READY**.

## Authority

- Canonical branch: main; continuing owner-designated Control Tower coordinates on Issue #1.
- Handoff: `docs/control-tower/handoffs/GRL-20260926-LIVE-MANUAL-UI-V49.md`.
- Sentinel: `END_OF_GRL_HANDOFF key=GRL-20260926-LIVE-MANUAL-UI-V49 sections=5`.
- Active task: Issue #21 / GRL-015.
- Active fallback packet: Issue #21 comment **`5843171488`**, FULL through `END_OF_GRL015_MANUAL_UI_FALLBACK key=GRL015-LIVE-MANUAL-UI-20260926 sections=6`.

## Status

PR #22 is merged; source work is complete.

External Computer Use refusal against GitHub Runner Local is an environment
UI-control limitation, not a source/product acceptance failure by itself.

Owner-manual UI fallback is authorized. The live operator must keep/take one broad
lease, ask the owner only for the exact visible wizard actions Computer Use cannot
perform, verify the resulting state, and continue automatically.

No new review/merge/CT gate is required because of UI-control denial.

## Live sequence

1. operator fresh checks/checkpoint;
2. owner manually Stop Now + confirm + close old wizard;
3. operator verifies ID 2 offline + Paused/no-survivor;
4. operator launches merged product;
5. owner manually signs in if needed, confirms exact private repo, clicks Resume existing runner once;
6. operator verifies SAME ID 2 online/no duplicate and runs one js-smoke PASS;
7. repeat planned Stop Now/close manually;
8. operator verifies checkpoint and asks owner for normal Windows Restart;
9. after boot operator resumes/launches product;
10. owner manually sign-in/repo/resume if Computer Use still denied;
11. operator verifies SAME ID 2 and runs second js-smoke PASS;
12. publish final Issue #21 evidence/release.

## Safety

No hidden bypass of Computer Use permission and no process-kill substitute for
Stop Now. ARCHIVE/CHECKPOINT FIRST.

No G1 rerun, PR #19, hosted Actions, service/autostart/security/sleep changes,
personal runner/Issue #18 yet, cross-repo/Stage-2/arbitrary jobs, global auth/env
reset, reinstall/unregister/reset/delete or `--replace`.

After both live witnesses pass: CT accepts Issue #21; Issue #18 switching is next.
