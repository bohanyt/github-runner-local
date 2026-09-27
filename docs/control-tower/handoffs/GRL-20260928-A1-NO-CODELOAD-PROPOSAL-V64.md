# github-runner-local — A1 codeload-free witness proposal (V64)

## 1. Phase

**GRL014_SOURCE_MERGED; A1_TEMP_NO_EXTERNAL_ACTIONS_OWNER_DECISION; A5_ODG_GATED; GRL015_ACCEPTED.**

Active proposal: Issue #18 `5860607984`, through:
`END_OF_GRL014_A1_NO_CODELOAD_PROPOSAL key=GRL014-A1-TEMP-NO-EXTERNAL-ACTIONS-20260928 sections=6`.

Owner reports the second device cannot use an independent network, so V63's proposed alternate-network discriminator is unavailable.

## 2. Retained diagnosis

Same-office-network second-device reproduction remains retained: codeload stalls reproduce beyond the runner laptop, while raw control stays healthy. This points more strongly toward a shared office-network/upstream codeload path, without proving a specific policy or infrastructure cause.

## 3. Proposed next step

Instead of waiting on codeload stability, propose a temporary private A1 witness workflow with **zero external `uses:` actions** and one harmless built-in shell marker step.

PASS run would prove started-hook -> marker execution ordering. REFUSE run would prove exit 73 suppresses the marker step.

No execution is authorized yet.

## 4. Owner decision

Required owner approval token:
`APPROVE_TEMP_NO_EXTERNAL_ACTIONS_A1_WITNESS`

## 5. Safety

Until approval: leave office ID3, `.env`, product and private workflow untouched. A5/OD-G/personal enrollment/cross-machine switching remain unauthorized.

END_OF_GRL_HANDOFF key=GRL-20260928-A1-NO-CODELOAD-PROPOSAL-V64 sections=5