# github-runner-local

**A Windows wizard for setting up your own GitHub Actions runner.**

Planned experience: open the app, sign in through GitHub, choose an authorized target and a local folder, register one runner, and receive build/test results back through GitHub.

> **Pre-implementation.** This repository currently contains project governance and a rough-design review lane. There is no working installer, published EXE, live runner, or completed Windows acceptance test.

## Start here

- Agents: [AGENTS.md](AGENTS.md), then [docs/CURRENT.md](docs/CURRENT.md).
- Product: [brief](docs/PRODUCT_BRIEF.md), [decisions](docs/DECISIONS.md), [platform references](docs/REFERENCES.md).
- Coordination: [Issue #1](https://github.com/bohanyt/github-runner-local/issues/1).
- Next gate: [GRL-001 / Issue #2](https://github.com/bohanyt/github-runner-local/issues/2), an independent planning pass. CURRENT selects the rough-design DRAFT PR.

## What this product is—and is not

The public repository is for distribution and development. It is **not a public remote-command mailbox** for a personal or workplace PC. Execution scope and credentials are configured separately.

GitHub is the request/result channel; the official Actions runner executes jobs locally. Logs and status return through Actions. Additional result comments/artifacts need explicit workflow implementation. Source changes are not automatically pushed back.

One repository-level runner does not automatically serve every repository in a personal account. A private execution hub is a design proposal, not an existing service. The next planner must settle scope and cross-repository credentials.

A folder is not a sandbox. Use only on authorized machines and for trusted code. See [SECURITY.md](SECURITY.md). A GUI or UAC approval does not override endpoint policy.

## Development state

No `.github/workflows` are installed during bootstrap or planning. Validation is local; no hosted Actions minutes are consumed. The owner will relay the planning prompt to Opus 5.5 in Claude chat before production implementation is assigned.

This is an independent project, not an official GitHub, OpenAI, or Anthropic product. License selection and code-signing arrangements remain open decisions. Public visibility is not a claim that a software license has already been selected.
