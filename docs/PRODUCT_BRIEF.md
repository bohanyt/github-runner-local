# Product brief

Status: owner requirements recorded on 2026-09-24; implementation choices remain proposed.

## Goal

Make a Windows laptop usable as a trusted GitHub-connected build/test executor without repeated manual terminal setup. Chat agents can request work through GitHub and read structured results there. Reduce dependence on GitHub-hosted runner minutes; do not sell this as unlimited compute or guaranteed faster builds.

## Accepted owner requirements

- Exact name: **github-runner-local**; public product/distribution repository.
- A real Windows GUI wizard, not `.cmd` or PowerShell as the primary operator interface.
- Browser/device GitHub sign-in and easy reuse when changing laptops/accounts.
- One runner first, one job at a time. Earlier three-runner ideas are superseded.
- Prefer one ordinary folder under Documents on C:. Detect redirected/OneDrive/network Documents and explain before choosing a local folder. Never silently move data or change folder policy.
- Request normal Windows UAC consent only when a specific administrative setup operation needs it; inspiration is the split UI/helper approach in Windows No Sleep.
- GitHub remains the request/result channel. The owner should not need to copy terminal logs back into chat.
- Durable AGENTS/CURRENT/Control Tower/task docs so another agent can continue the complete project.
- Rough design first; an independent Opus 5.5 planning pass next; implementation later.

## User experience target

Welcome/preflight → GitHub sign-in → execution scope → machine/folder → mode and permission review → install/register → explicitly approved smoke test → dashboard.

Dashboard should show selected account and scope, exact registered runner, online/idle/busy/offline state, current job, disk budget, latest result, pause/drain, disconnect and diagnostics. Labels, paths and credentials must not be conflated.

## Resource assumptions

Design for one Windows x64 laptop with constrained free disk (approximately 45 GB in the motivating case). RAM and exact CPU model are not measured. Use conservative sequential jobs, bounded logs/caches and headroom checks. GPU acceleration is not a baseline CI requirement. Benchmarks, not core count or GPU branding, determine speed claims.

## Not in the first release

No general remote shell, arbitrary command text, untrusted/public-fork execution, automatic source push, AI model hosting, unattended privileged jobs, fleet autoscaling, Linux container parity on native Windows, or game/desktop-interaction acceptance. No automatic modification of other repositories' workflows.

## Unresolved conflicts

A safe service/helper may need protected files outside user-writable Documents. Preserve the folder preference for portable mode; ask for owner approval before adopting an installation layout that departs from it. A public source repository does not authorize public jobs on the laptop.
