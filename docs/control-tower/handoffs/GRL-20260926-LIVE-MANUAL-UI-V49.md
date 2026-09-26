# github-runner-local — live acceptance with owner-manual UI fallback (V49)

## 1. Phase

**G1_OFFICE_PASS_CT_ACCEPTED; GRL015_LIVE_MANUAL_UI_FALLBACK_READY.**

PR #22 is merged. Live acceptance remains active on Issue #21.

External Computer Use denial reported in `5843149380` is classified as an
environment UI-control limitation, not a product failure.

Active fallback packet: Issue #21 comment `5843171488`, read IN FULL through:
`END_OF_GRL015_MANUAL_UI_FALLBACK key=GRL015-LIVE-MANUAL-UI-20260926 sections=6`.

## 2. Live operator model

ONE broad local operator lease continues through:
planned close/relaunch -> js-smoke PASS -> planned reboot -> js-smoke PASS.

The operator performs all build/shell/API/GitHub/evidence work.

When the external Computer Use layer refuses the wizard, the owner performs only
the visible product UI clicks requested by the operator. This is authorized
acceptance evidence and does not trigger another review/merge/CT loop.

A UI-control denial alone must not cause another blocker handoff.

## 3. Manual UI interactions

Owner UI A:
Stop Now -> confirm warning -> close old wizard -> reply done.

Operator verifies same ID 2 offline and persisted Paused/no-survivor.

Operator launches merged product.

Owner UI B:
sign in if shown -> exact private repo -> Resume existing runner once -> reply done.

Operator verifies same ID 2 online/no duplicate and runs js-smoke PASS.

Then owner UI C:
Stop Now -> confirm -> close -> reply done.

Operator verifies checkpoint and tells owner ready for planned Windows Restart.

After reboot:
operator resumes authority/launches product; owner UI D/E signs in if needed and
clicks Resume existing runner once; operator verifies same ID 2 and runs second
js-smoke PASS.

## 4. Safety

No hidden bypass of the external Computer Use permission. No process kill as a
substitute for product Stop Now. No reinstall/unregister/reset/delete/replace.

ARCHIVE/CHECKPOINT FIRST. Preserve runner/root/credentials/private history.

G1 stays closed. No PR #19, hosted Actions, service/autostart/security/sleep
changes, personal runner, cross-repo/Stage-2/arbitrary jobs, or global auth/env reset.

## 5. Finish line

REOPEN_RELAUNCH_PASS + GRL015_REOPEN_REBOOT_PASS -> CT accepts Issue #21 ->
Issue #18 switching next.

END_OF_GRL_HANDOFF key=GRL-20260926-LIVE-MANUAL-UI-V49 sections=5
