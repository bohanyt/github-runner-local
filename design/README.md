# Rough design v0

**PROPOSED / DISCUSSION ONLY. No installer, authentication, UAC, runner, or workflow is implemented here.**

Start with main `AGENTS.md` and `docs/CURRENT.md` in the repository. This pack is the candidate for GRL-001 / Issue #2, not an implementation authorization.

## Read the whole pack

- [Architecture](ARCHITECTURE.md): topology, identity boundaries, and open choices.
- [Wizard](WIZARD.md): proposed screens and lifecycle behavior.
- [Threat model](THREAT_MODEL.md): what a GUI, profile, or private repo does not protect.
- [Request/result protocol](REQUEST_RESULT_PROTOCOL.md): the proposed GitHub round-trip.
- [Acceptance](ACCEPTANCE.md): staged proof, failures and owner gates.
- [Offline preview](wizard-preview.html): clickable mock UI, no network or machine actions.
- [Request fixture](examples/request.json) and [result fixture](examples/result.json): invented examples, never real evidence.

## What to do with this

Opus 5.5 should challenge this proposal, settle or explicitly block its open decisions, and publish one complete planning report on Issue #2. It must not treat the prototype as a selected UI framework or start installing software. No production `.github/workflows` are included.

The proposal favors one private-repository pilot first, then a separately approved private execution hub if multi-project use is required. That is a recommendation, not a silent reduction of the eventual multi-project goal. Public distribution remains independent from both.

## Local checks

With Python 3.10+ available, from the repository root, `python tools/check_design.py` checks the design pack and invented fixtures. `python -m unittest discover -s tools -p 'test_*.py'` tests the checker. These are documentation/prototype checks, not an implementation of dispatch, authentication or job isolation.

Open `design/wizard-preview.html` in a browser to explore the mock. Its actions are explicitly simulated. It neither requests a token nor invokes UAC.

Optional browser check: with Playwright and compatible Chromium already installed, run `python tools/check_preview.py --chromium <browser-path>`. It injects the HTML through a browser test harness, checks interactions and narrow/desktop layouts, and writes only local evidence. It does not prove Windows file-launch compatibility.
