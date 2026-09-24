# github-runner-local — A1 review-ready handoff V4

## 1. Phase

A1 implementation is published and locally checked. Phase: **A1_IMPLEMENTED / AWAITING_INDEPENDENT_EXACT_HEAD_REVIEW**. Nothing is merged.

## 2. Authority

Read main `AGENTS.md` → `docs/CURRENT.md` → this handoff through the end marker → Issue #1 latest claims → Issue #4 implementation evidence → Issue #6 review packet.

## 3. Exact implementation candidate

- DRAFT PR #5, open/unmerged.
- Branch: `feat/grl-a1-core-contracts`.
- Base: `3e02566f5dcaeea01f5f34284e21731c71b1144b`.
- Exact head: `ae2e23a156695f19d9511736a7758b53d4a1406c`.
- Handoff: Issue #4 comment `5808741650`.
- Implementation claim `5808409504` released by `5808748533`.

If PR #5 head moves, the review target is stale.

## 4. Evidence

Worker-local Windows 10.0.26200 / .NET SDK 10.0.401:
- build with warnings-as-errors passed;
- 189/189 tests passed, 0 skipped;
- no-restore/no-build test rerun also passed.

Classification: `LOCAL_CHECKED` only.

## 5. Review authority

Issue #6 is GRL-003, the complete independent exact-head review packet. Reviewer must read the full PR diff and all changed files, not rely on PR summary or worker handoff alone.

Focus includes exact A1 scope, state-machine semantics, strict settings, explicit host-independent Windows path rules, request/result/schema consistency, negative tests and evidence quality.

## 6. Reviewer boundary

Reviewer is not CT and not the implementation worker. Read-only source review. Allowed writes are claim, one review comment on Issue #4, and release. No source edits, PR changes, merge, Actions, machine setup or later checkpoints.

No GitHub-hosted Actions are authorized. Local reproduction is optional only if the reviewer environment already supports it; lack of .NET does not excuse source review.

## 7. Accepted review dispositions

Exactly one:
- `PASS_A1_EXACT_HEAD`
- `NEEDS_A1_CORRECTION`
- `STALE_REVIEW_TARGET`
- `BLOCKED_REVIEW`

PASS does not itself authorize merge. CT/owner evaluates the result afterward.

## 8. Next

One independent reviewer may claim GRL-003 and complete the review. No reviewer has been launched by CT. Do not start checkpoint B or later work.

END_OF_GRL_HANDOFF key=GRL-20260924-A1-REVIEW-READY-V4 sections=8
