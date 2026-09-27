# github-runner-local — A1 ID3 network-gated retry authorized (V60)

## 1. Phase

**GRL014_SOURCE_MERGED; A1_ID3_NETWORK_GATED_RETRY_AUTHORIZED; A5_ODG_GATED; GRL015_ACCEPTED.**

Active retry packet: Issue #18 `5857096676`, through:
`END_OF_GRL014_A1_NETWORK_RETRY_PACKET key=GRL014-A1-ID3-NETWORK-GATED-RETRY-20260927 sections=6`.

Prior Opus attempt `5857026595` was BLOCKED by codeload action-download timeout before execute started-hook. Checkpoint and rollback succeeded; request count 1; same ID3 restored online/idle.

## 2. Retained partial A1 evidence

Real root `.env` -> pinned Listener -> pinned Worker -> started hook was empirically observed for admit/report/verdict. Admit hook ordering before checkout was observed.

Still pending: execute PASS hook -> js-smoke profile, and execute exit-73 refusal before default/profile steps.

## 3. Retry gate

Before `.env` mutation or witness request, the operator must perform two consecutive successful scratch downloads of all pinned external action archives used by the workflow, with comfortable margin under the observed 100s timeout. Any failure or >60s archive => BLOCKED with no live mutation.

After hook install/restart, one more network health round is required immediately before request #1.

Do not prewarm or mutate the runner action cache.

## 4. Operator

Opus local Windows is preferred. Same successful Opus session may resume with a new explicit goal, or a fresh Opus session may be used.

Product/wizard MUST NOT be launched from the Claude/agent shell. Owner performs all product stop/start/resume from normal Explorer/desktop path; operator observes afterward.

Same owner-authorized live scope remains: ID3 only, exactly two new js-smoke requests PASS then REFUSE if prerequisites hold, reversible hooks/.env, byte-exact rollback.

## 5. Safety

A5/OD-G/personal enrollment/cross-machine switching remain unauthorized.
No proxy/DNS/firewall/domain/security changes, re-registration, reboot, workflow/profile mutation, third request or hosted Actions.
ARCHIVE/CHECKPOINT FIRST.

END_OF_GRL_HANDOFF key=GRL-20260927-A1-ID3-NETWORK-GATED-RETRY-V60 sections=5