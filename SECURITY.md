# Security and trust

This project is in design, not deployment. No runner is configured by the current repository.

The proposed product executes code with the runner's operating-system permissions. A workspace folder, private repository, profile allowlist, non-admin account or encrypted credential store is not a complete sandbox. Same-user processes can share access to sensitive resources. For a workplace machine, obtain authorization and do not expose company data/network services to builds.

Public distribution must remain separate from private/authorized execution. Never automatically run public fork contributions on a personal/workplace host. Authenticated requesters can still submit harmful code; review target provenance and dependency behavior, minimize credentials and prefer real isolation where needed.

GUI consent, GitHub OAuth and Windows UAC are separate. Do not run jobs as Administrator/LocalSystem merely to simplify setup. Elevated helpers must accept only bounded validated operations and protect privileged executables from replacement by lower-privileged jobs. Do not disable endpoint controls or hide blocked subprocesses.

Do not publish credentials or real private logs. Report design concerns through an issue only after removing sensitive material. If GitHub private vulnerability reporting is available for this repository, use it for sensitive reports; its configuration has not been claimed here. Never paste tokens into chat or an issue. Detailed threat-model proposals live in the design PR selected by main CURRENT.
