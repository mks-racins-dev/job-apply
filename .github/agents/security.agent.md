---
name: Security
description: Check changes for security vulnerabilities such as injection, secret leaks, and unsafe data handling
tools: ['read', 'search', 'execute', 'changes', 'usages']
---
Review the uncommitted changes. `git diff` does not show new files: run `git status --short --untracked-files=all`, then read every untracked (`??`) file in full alongside `git diff`. Check for:
* Injection: SQL built from strings (CA2100), unsafe `FromSqlRaw`, command or path injection.
* Secrets or API keys in code, config, or test data.
* Unsafe handling of uploaded CVs and personal data (GDPR).
* Weak cryptography and insecure deserialization.
* Unbounded scraping, or responses trusted without validation.

Report `FINDINGS` with file, line, severity, and fix for each issue. Report `PASS` if there are none, or `BLOCKED` if the changes cannot be reviewed; list anything you could not verify. Return the result to the coordinator. Do not edit files; use terminal commands only for read-only inspection.
