# github-runner-local — real dogfood first; codeload/cache escape hatch retained (V65)

## 1. Phase / authority

**GRL014_SOURCE_MERGED; REAL_DOGFOOD_FIRST; A1_PENDING; ACTION_CACHE_ESCAPE_HATCH_ONLY; A5_ODG_GATED; GRL015_ACCEPTED.**

Canonical branch: `main`.

Active CT adjudication: Issue #18 comment `5881385625`, through:
`END_OF_GRL014_REAL_DOGFOOD_ADJUDICATION key=GRL014-REAL-DOGFOOD-FIRST-20260929 sections=6`.

V64 synthetic no-external-actions A1 proposal `5860607984` remains valid but is PARKED, not deleted or accepted for execution.

## 2. Retained completed GRL work

Do not reopen without a concrete regression:
- G1 accepted;
- GRL-015 accepted;
- office runner `grl-office` numeric ID 3 accepted for the existing office-only lane;
- PR #24 / GRL-014 source implementation merged and accepted;
- F1/F2/F3/F4 source correction loop closed;
- prior real A1 evidence for root `.env` -> pinned Listener -> pinned Worker -> started hook on admit/report/verdict retained;
- admit hook-before-first-checkout ordering retained;
- checkpoint/rollback procedure retained.

A1 is NOT complete: execute PASS/profile and execute refusal remain unproved. A5/OD-G, personal enrollment and cross-machine activation remain gated.

Last accepted live office-ID3 end-state is still the restored online/idle result from Issue #18 `5857287220`; V65 does not freshly observe or operate the laptop.

## 3. Codeload diagnosis / what it means

Runner-laptop diagnosis `5857817648`: repeated identical public `codeload.github.com` requests intermittently returned HTTP 200 and then stalled mid-body; raw.githubusercontent.com control remained healthy.

Second-device same-office-network record `5858205161`: codeload 6/10 complete, 4/10 HTTP-200/curl-28 partial-body timeouts; raw control 10/10 complete. This materially reduces a runner-laptop-only explanation.

Do NOT upgrade this to a proved company-policy, endpoint-security, router, ISP or GitHub-root-cause claim. The second device cannot use an independent network, so that A/B route is unavailable and should not remain a gating requirement.

Do not repeat another hour-long same-network/protocol matrix without a new concrete question.

## 4. Opus codeload-workaround consultation

Owner supplied an Opus consultation performed scratch-only, with no live GRL mutation. Preserve these findings as advisory evidence:
- exact pinned runner v2.337.0 supports `ACTIONS_RUNNER_ACTION_ARCHIVE_CACHE` on Windows without rebuilding/patching the runner;
- cache layout is action repository + resolved exact SHA ZIP;
- the runner only consumes a pre-populated cache; it does not safely provision or integrity-bind it for us;
- cache miss/read-copy failure falls back to network;
- a corrupt cache archive can hard-fail extraction instead of falling back;
- runner does not strongly verify cached ZIP content against the pinned commit, so any provisioning path needs its own integrity/provenance verification;
- Opus scratch reported Git fetch exact SHAs 3/3 successful, `git archive` + verifier 3/3 successful;
- one bounded codeload campaign had `download-artifact` fail 5/5;
- HTTP Range/resume was not usable in that test;
- alternate GitHub archive/zipball/tarball URL forms redirected back to codeload.

CT disposition: **ACTION CACHE IS AN ESCAPE HATCH, NOT DEFAULT PRODUCT SCOPE.** Do not build a downloader/cache subsystem merely because the office path is weird. Use it only if real dogfood repeatedly proves ordinary useful jobs are blocked by codeload.

No cache is installed on office ID3.

## 5. Owner product direction — dogfood real work

Owner explicitly prefers using GRL for real project work instead of accumulating more synthetic proof and complexity.

First dogfood rules:
- trusted code only;
- one fixed/bounded workload contract, not arbitrary remote command execution;
- non-destructive;
- preferably secretless;
- build/lint/test/local smoke are good;
- no deploy, publish, social posting, WordPress posting, real Drive mutation or production OAuth mutation;
- project-specific authority remains authoritative; GRL CT must not hijack another project's acceptance or branch rules.

Use real dogfood outcomes to answer whether GRL is actually useful and whether codeload mitigation is truly necessary.

## 6. Cross-project readiness snapshot

### KIN Marketing / KIN MCP

`karyainformasinusantara/marketingkin` Issue #11 comment `5861919010` records owner direction to prepare a future Windows workload contract but explicitly says **NOT DOGFOOD_READY yet**.

Fresh snapshot at V65 publication: `marketingkin@main` remains `315c7e40eb42681cf1535b4c67bf0f8ec0365c7f`; publisher/KIN-MCP implementation has no authorized executable repo/branch/exact SHA yet.

Do not invent a `kin-mcp-check` profile before a real `DOGFOOD_READY_HANDOFF` exists.

### Recantor

`bohanyt/recantor` is currently the closest serious Windows candidate, but obey Recantor authority:
- Issue #63 / DRAFT PR #65 head: `2a4aa7f4ab81533ffe271a5266079b0d27ade55b`;
- R63-C2 result `5863606937`: CLEAN; `WINDOWS_UPDATER_WITNESS: MAY_PROCEED`; no Actions rerun;
- CT preflight `5863693528`: final normal-client witness blocked on approved remote pullability/distribution of exact candidate images;
- local Windows pre-witness `5865881019`: PASS for reviewed updater/runtime behavior on Windows + Docker Desktop, but explicitly NOT final release-distribution acceptance.

Therefore Recantor is promising dogfood, but do NOT duplicate its already-earned local witness, bypass its release-distribution gate, or silently turn GRL into its acceptance executor. Fresh-check Recantor before proposing any GRL workload.

## 7. Immediate successor decision

**SEQUENTIAL:**
1. Fresh-read V65 + Issue #18 adjudication + latest Issue #1 claims.
2. Fresh-check candidate projects instead of creating synthetic work.
3. If a real project publishes a truthful bounded Windows workload contract, evaluate it as the next GRL dogfood.
4. Prepare the smallest fixed GRL execution/profile change needed, if any.
5. Before any execution-hub/workflow/profile/live-runner mutation, obtain explicit owner approval and checkpoint first.
6. Execute only one bounded real workload; classify failures honestly.
7. Only if repeated real work is blocked specifically by codeload, revisit the native action-archive-cache escape hatch.

If no real workload is ready, do not manufacture one. V64 no-external-actions A1 remains a fallback proposal, not the preferred next step.

## 8. Hard boundaries / handoff invariant

Until new explicit owner approval:
- no GRL request;
- no private execution-hub workflow/profile mutation;
- no action-cache install/provisioning;
- no ID3 root/`.env`/registration/product mutation;
- no runner stop/restart for A1;
- no network/security/proxy/DNS/AV/EDR changes;
- no personal runner enrollment or switching;
- no A5/OD-G;
- no GitHub-hosted Actions dispatch/rerun while the standing hold remains;
- no cross-project source write merely to create dogfood.

ARCHIVE/CHECKPOINT FIRST before any future persistent or destructive action. Reuse existing evidence and avoid redundant rereads/tests.

END_OF_GRL_HANDOFF key=GRL-20260929-REAL-DOGFOOD-FIRST-V65 sections=8