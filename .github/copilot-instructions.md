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
- Planner delegates only to Coder. Coder implements the plan and coordinates Tester, Docs, Reviewer, Security, and Release, advancing only after each required stage succeeds. Pass each worker the context it needs; handoff buttons are not automatic delegation.
- Coder fixes actionable findings and stops after 3 fix rounds if findings remain. Revalidate and rereview after a fix.
- Do not wait between stages unless a decision or tool approval is required. Stop if delegation or a required tool is unavailable.

## Running the factory
- Start a new VS Code chat with the repository open, select the Copilot harness and Planner, and describe the feature. Enable the agent tool when needed.
- Planner invokes Coder; Coder needs nested subagent delegation to run the remaining stages. If the harness does not provide it, stop and report the blocker. Alternatively, start a new chat with Coder and supply the approved plan.
- For the Local harness, enable `chat.subagents.allowInvocationsFromSubagents` and the Run Subagent tool. Local handoffs are not automatic delegation.
- Tool approvals and authentication may require interaction. Instructions guide agents but do not guarantee unattended execution or enforce permissions; verify the results before approving the PR.
