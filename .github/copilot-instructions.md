# Job Magnet: project instructions for all agents

Job Magnet reads user profile, scrapes Latvian job boards, and uses Claude to score matches. See `PLAN.md` for the product and architecture.

## Layout
- `backend/`: .NET 10 solution `JobMagnet.sln` (Api, Worker, Scrapers, Matching, Data, Web, Tests).
- `learning/`: local study notes, excluded from git. Do not change it.

## Commands (run from `backend/`)
- Build: `dotnet build JobMagnet.sln`
- Test: `dotnet test JobMagnet.sln`

A change is done only when the build has 0 errors and all tests pass.

## Code standards
- Shared settings live in `backend/Directory.Build.props`. Warnings are errors (`TreatWarningsAsErrors`). Do not disable a rule without a written reason in the same file.
- NuGet versions live only in `backend/Directory.Packages.props`. Never add `Version=` to a `PackageReference`.
- Do not create `packages.lock.json` files.
- EF migrations under `**/Migrations/*.cs` are generated. Do not edit them.
- Use `CultureInfo.InvariantCulture` for number and date formatting. Latvian text needs culture-safe comparisons.
- Use `[LoggerMessage]` for logging in services.
- Only comment code that needs clarification.

## Factory rules
- Do all work on a feature branch created from `main`. Never commit to, push to, or force-push `main`. Never merge a pull request.
- The Release agent commits and pushes only after the build and tests pass and Reviewer and Security report no open findings. It then opens a pull request into `main`.
- The user approves the pull request. That approval is the final gate.
- Stay within the task. Do not fix unrelated problems.
- Planner coordinates Coder, Tester, Reviewer, Security, and Release as subagents, advancing only after each stage succeeds. Pass each worker the context it needs; handoff buttons are not automatic delegation.
- Planner counts returns to Coder for fixes. After 3 fix rounds, stop and report any open findings.
- Do not wait between stages unless a decision or tool approval is required. Stop if delegation or a required tool is unavailable.

## Running the factory
- Start a new VS Code chat with the repository open, select Planner, and describe the feature. Enable the agent/subagent tool for the chosen harness if needed.
- The coordinator plans, delegates sequentially, and reports the opened PR for your approval. Tool approvals and authentication may still require interaction; agent instructions are not a guarantee of unattended execution.
