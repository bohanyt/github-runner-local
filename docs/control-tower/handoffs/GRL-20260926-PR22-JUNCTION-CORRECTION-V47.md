# github-runner-local — PR #22 junction correction (V47)

## 1. Phase and authority

**G1_OFFICE_PASS_CT_ACCEPTED; GRL015_PR22_JUNCTION_CORRECTION_READY.**

Active task remains Issue #21 / GRL-015. Active correction packet is Issue #21
comment `5842873213`, read IN FULL through:
`END_OF_GRL015_CORRECTION_PACKET key=GRL-PR22-JUNCTION-R1-20260926 sections=6`.

Independent review `5842855807` / release `5842857983` reviewed PR #22 exact
head `7d4ff0f07d4b93562932469b48d1310069f8dc91` and found exactly one blocking
issue: R-GRL015-1 SAFETY_BLOCKER, a Windows `bin` junction bypass in the
existing-root listener path.

## 2. What remains accepted

Reviewer independently passed:
- Integration 170/170;
- Presentation 64/64.

No additional REOPEN_PATH_BLOCKER or DEFERRED finding was reported.

Retain accepted behavior for planned-pause eligibility, exact remote identity,
offline/non-busy precheck, no configure/registration token/replace, concurrency,
truthful failure states, no adoption of unowned online process, UX eligibility,
first-install provenance and removal provenance.

G1 remains CT-accepted and closed.

## 3. Narrow correction

SAME local Opus continues SAME branch / SAME DRAFT PR #22 from old head
`7d4ff0f07d4b93562932469b48d1310069f8dc91`.

Primary source change:
- `src/Grl.Integration/PortableRunnerProcess.cs`

Targeted regression:
- `tests/Grl.Integration.Tests/PortableLifecycleTests.PlannedReopen.cs`
- shared test helper file only if mechanically needed.

Fix the full reopened listener execution path so `root\bin\Runner.Listener.exe`
cannot traverse a directory junction/reparse point. Preserve/immediately recheck
the ordinary root + run.cmd boundary before starting.

No presentation/lifecycle redesign, no NF-2 expansion, no live runner work.

## 4. Evidence and rereview

Implementer runs full Integration tests, focused regression, diff/path checks and
normal fast-forward push to SAME PR #22, then publishes exact-head handoff and
releases claim.

Then ONE focused independent rereview, preferably same reviewer, validates only
the correction and retains prior conclusions for unchanged code.

No Stop Now, close/relaunch or reboot before PASS and separate CT merge/resume.

## 5. Sequence / safety

**SEQUENTIAL:** narrow correction -> focused rereview -> CT merge/resume -> SAME
Opus live close/relaunch -> planned reboot -> Issue #18.

ARCHIVE/CHECKPOINT FIRST. Preserve runner/root/credentials/history and old PR
head. No hosted Actions, service/autostart, security/sleep changes, personal
runner, Stage-2/cross-repo/arbitrary jobs or global auth/env reset.

END_OF_GRL_HANDOFF key=GRL-20260926-PR22-JUNCTION-CORRECTION-V47 sections=5
