# github-runner-local — normal positive passed; anchorless final G1 (V44)

## 1. Phase and authority

**NORMAL_POSITIVE_PASS; FINAL_MISMATCH_READY; G1_INCOMPLETE.** Active Issue #15 packet is `5841981163`, FULL through `END_OF_GRL_FINAL_G1_PACKET key=GRL-OFFICE-G1-ANCHORLESS-FINAL-20260926 sections=5`. CT claim Issue #1 `5841974558` must be released before the local operator claims.

Public main includes reviewed NF-1 through PR #20 merge `1aa9a91695029bc461b0e38ab180dc3ee8959a5d`. PR #19 remains draft/unmerged at `f911e7be18d7c574947416609be4c797c360eada`; its witness tool is acceptance-only evidence, not shipping product code.

## 2. Live evidence already complete

Issue #15 `5841913208`, claim/release `5841867433` / `5841914467`: private main fast-forwarded to `dfb1dfd6b41d2ae951ae555ec751be2527230742`, root tree `4855ddca0143de2e0e21d95946b0e1b465b35143`; one ordinary request `5841882572` produced ACK `5841886011`, run `36208004263`, result `5841899241`, successful `grl/js-smoke` status and successful verdict on grl-office ID 2, v2.337.0, non-elevated. Disposition NORMAL_POSITIVE_PASS / G1_INCOMPLETE.

Historical prefilter negative run `36202109916` remains accepted: workflow prefilter blob is unchanged and all four jobs were skipped with no runner/ACK.

## 3. Anchorless mismatch decision

Independent review `5841741557` closed W-1 interruption/false-normal behavior and found only S-1: the runbook's raw anchor commit/push preceded destination and clean-state checks.

CT removes that operation entirely for this G1. No source correction or new reviewer loop.

Use exact reviewed tool from PR #19 head `f911e7be18d7c574947416609be4c797c360eada`, unmodified, with:
- P = current private main `dfb1dfd6b41d2ae951ae555ec751be2527230742`;
- R = existing ancestor `3ca0449e39fdc28cf5ca15b30967833a95649192`;
- O = `dfb1dfd6b41d2ae951ae555ec751be2527230742`;
- fresh UUIDv4 nonce.

CT verified R is the direct ancestor of O, has no existing commit statuses, and R/O share workflow blob `71e0271ee9c1146ebcdc2ac9d982b32c1a773b5d` and profile blob `2bb80439a8829355887b9125d3a4fe6f6cbdee64`. The reviewed tool already permits distinct requested/observed ancestor commits. No anchor commit/push is performed.

## 4. One broad local lease and finish line

After CT release/no competing claim, SAME local Opus takes one claim for the whole remaining G1 operation: fresh state/destination/queue checks -> checkpoint -> plan directly at P -> apply/verify overlay -> exactly one FAULT_INJECTED request targeting R with observed O -> collect refusal/status/verdict -> restore/verify until NORMAL_CONFIRMED -> publish final evidence/release.

Do not stop at micro-milestones. No source patch, PR #19 merge, new review, generic test campaign or runner reinstall is required unless a genuinely new blocker appears.

If live mismatch proves BLOCKED/CHECKOUT_SHA_MISMATCH with zero profile execution/no canonical result, reporting/status/verdict are correct, and the actual private repo is restored NORMAL_CONFIRMED, the operator may publish **G1_OFFICE_PASS** using the normal positive + historical prefilter + new mismatch proof.

If restoration or witness semantics fail, do not manufacture PASS; publish the precise blocker. Preserve all state and use only reviewed idempotent recovery.

## 5. Boundaries and next product phase

No hosted Actions, service/UAC/security/sleep-policy changes, arbitrary/cross-repo/Stage-2 jobs, personal runner, global auth/env reset or implicit Stop Now/unregister/reboot/reinstall. ARCHIVE/CHECKPOINT FIRST; keep secrets/local machine details private.

After accepted G1: prioritize reliable app/runner reopen/restart, then separate Issue #18 one-active-at-a-time office↔personal switching. NF-2 remains deferred.

END_OF_GRL_HANDOFF key=GRL-20260926-ANCHORLESS-FINAL-G1-V44 sections=5
