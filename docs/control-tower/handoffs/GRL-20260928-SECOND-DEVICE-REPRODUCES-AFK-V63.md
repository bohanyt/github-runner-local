# github-runner-local — second-device reproduction; owner AFK hold (V63)

## 1. Phase and authority

**GRL014_SOURCE_MERGED; SECOND_DEVICE_REPRODUCES_REPORTED; A1_NETWORK_BLOCKED; OWNER_AFK_HOLD; A5_ODG_GATED; GRL015_ACCEPTED.**

CT evidence record: Issue #18 comment `5858205161`, through:
`END_OF_GRL_HANDOFF key=GRL014-SECOND-DEVICE-REPRODUCES-OWNER-AFK-20260928 sections=5`.

Pre-publication authority checkpoint: main `315c162a042036862dfc115f29e2ecac53a1c7df`, CURRENT V62. V62 and all earlier source/evidence remain preserved in Git history.

The owner requested the second-device diagnostic, confirmed same office Wi-Fi/SSID in chat, forwarded its result, and then said they are going AFK to sleep. Device-2 was deliberately instructed to make no GitHub writes. Do not invent a device-2 claim, prior approval-token comment or independent CT execution.

## 2. Reported result and evidence level

Owner-forwarded LOCAL_CHECKED report, not CT-reproduced evidence. Device-2 raw logs and full hash values have not been inspected by CT.

Exactly 10 codeload checkout ZIP GETs interleaved with 10 raw control GETs, 90-second limit, no retry:
- codeload: 6/10 completed, 4/10 mid-body timeouts; successful min/median/max 0.530 / 0.842 / 74.395 seconds;
- failed request numbers 2/5/6/7: HTTP 200 and curl exit 28 at about 90 seconds; partial bytes 147470 / 168630 / 79860 / 136138 of 480901;
- raw control: 10/10 completed; min/median/max 0.348 / 0.718 / 1.244 seconds;
- hashes reportedly consistent within each target; full digests not supplied;
- 441 reported network checks found no disconnect/change;
- stale repo unchanged (remote URL read initially); no runner install, settings change or GitHub write.

A completed 74.395-second transfer is not a fast/stable A1 network-gate success.

## 3. Interpretation and retained progress

The repeated codeload-only stall pattern on a second device reduces the runner-laptop-only hypothesis and points more strongly toward the shared office-network/upstream path. This does not identify a specific company policy, security product, ISP/router or codeload fault. Same-SSID confirmation does not itself prove identical lower-level routing, and the campaigns were not simultaneous.

Prior runner-laptop diagnosis `5857817648` remains retained, alongside partial A1 hook inheritance/ordering evidence in `5857026595`.

G1, GRL-015 and PR #24 merged-source acceptance stay complete. Do not repeat source reviews or same-network diagnostic campaigns without a concrete new question. A1 execute PASS/profile and execute refusal still lack proof. A5/OD-G and personal enrollment remain gated.

## 4. Owner AFK hold

Do not dispatch a new worker, retry A1, post GRL requests, change hooks/.env, stop/restart runner/product, switch networks, seed caches, alter security/ACL settings or schedule unattended polling while the owner is away.

Leave office ID 3 untouched. Last accepted operator state is restored online/idle in `5857287220`; this handoff is not a fresh live-state observation.

Preserve all checkpoints, proof hooks, logs, worktrees and device-2 scratch measurements. Do not require a new approval phrase or another copy/paste task from the sleeping owner. Release CT coordination ownership after publication.

## 5. Resume point

After the owner returns, proposed next discriminator: the same second device and existing bounded probe on an owner-approved independent connection, keeping the runner laptop untouched. This is not dispatched or authorized by the AFK hold. Do not disable corporate controls to test it.

Reuse prior evidence; no new hour-long protocol matrix or blind install/restart cycle. First obtain consent to the concrete next action, then state its order and bounded finish condition. A1 is not automatically reopened by this result.

END_OF_GRL_HANDOFF key=GRL-20260928-SECOND-DEVICE-REPRODUCES-AFK-V63 sections=5
