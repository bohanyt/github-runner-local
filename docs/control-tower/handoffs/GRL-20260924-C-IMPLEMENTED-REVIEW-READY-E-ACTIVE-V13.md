# github-runner-local — C implemented/review-ready; E active (V13)

## 1. Phase

**C_IMPLEMENTED / AWAITING_INDEPENDENT_EXACT_HEAD_REVIEW + E_SOURCE_IMPLEMENTATION_ACTIVE.**

Checkpoint C source is complete and published on DRAFT PR #12. Checkpoint E remains independently claimed by a separate worker.

## 2. Authority/read order

Read main `AGENTS.md` → main `docs/CURRENT.md` → this handoff → Issue #1 latest claims → relevant task issue.

C task: Issue #10 / GRL-009.
E task: Issue #11 / GRL-010.

## 3. C exact candidate

Branch:
`feat/grl-c-device-runner-package`

DRAFT PR #12:
open, draft, unmerged, mergeable.

Base:
`cd6f6abd13de9af2122d1c61db774b2dc54f7da0`

Head:
`67dd220771bf67f65348e622998e005bf30b26bc`

Implementation handoff:
Issue #10 comment `5813138169`.

Implementation release:
Issue #1 comment `5813145195`.

Candidate diff is one commit / 13 paths and fits Issue #10's allowlist.

## 4. C evidence

Reported worker-local Windows evidence:
- build: PASS, 0 warnings/errors;
- Core: 192/192;
- Presentation: 45/45;
- Integration: 45/45;
- zero failed/skipped;
- RunnerContractProbe: PASS;
- runner v2.337.0 asset/hash matched reviewed official pin;
- Listener version: 2.337.0;
- config help: all 12 required capabilities.

No runner registration/start, live login, GitHub App creation, private execution repo, hosted Actions, service/UAC, D, or release work occurred.

Evidence: `LOCAL_CHECKED`; official pin metadata: `SOURCE_VERIFIED`.

## 5. C independent review scope

A different reviewer must inspect exact head
`67dd220771bf67f65348e622998e005bf30b26bc`
against the FULL Issue #10 packet.

Review all 13 paths, especially:
- injected HTTP/device-flow state machine and token redaction;
- admin/private repository gating;
- package pin/hash/URL trust;
- safe extraction against rooted/traversal/ADS/device/symlink/reparse/size/cleanup attacks;
- no deletion outside owned roots;
- CLI capability/version policy and no auto `--replace` / `--disableupdate`;
- quality and reach of deterministic tests;
- contract probe source: only read-only runner version/help calls, no registration/start;
- evidence coherence and exact scope.

Review may use local/cloud reproduction if available but must not dispatch hosted Actions.

## 6. C review outcomes

Publish one verdict on Issue #10:

- `PASS_C_EXACT_HEAD`
- `NEEDS_C_CORRECTION`
- `STALE_C_REVIEW_TARGET`
- `BLOCKED_C_REVIEW`

If PASS, record exact reviewed head, findings=0, evidence limitations, and that merge remains a separate CT/owner action.

Do not merge in the review role.

## 7. E parallel state

Active E implementation claim:
Issue #1 `5812970520`.

E remains limited to:
`templates/execution-repo/**`.

E-B1 resolution:
Issue #11 `5812014790`.

This V13 continuity commit is docs/authority-only and does not overlap E's implementation write scope. E may continue from its selected base without silently rebasing merely because main continuity advanced.

## 8. Remaining gates

OD-1 private execution repo: OPEN.
OD-2 GitHub App creation/visibility: OPEN.
D21 Stage-2 credential authority: OPEN.
Service/signing gates remain OPEN.

No C merge, D, live E activation, G1, F, or H is authorized yet.

## 9. Next

One independent reviewer claims the C exact-head review, reviews DRAFT PR #12, publishes one Issue #10 verdict, releases the claim, and stops. E continues independently under its own existing lease.

END_OF_GRL_HANDOFF key=GRL-20260924-C-IMPLEMENTED-REVIEW-READY-E-ACTIVE-V13 sections=9
